using CUETools.Processor;
using CUETools.Wpf.Services;

namespace CUETools.Linux.App.Services;

/// <summary>One proposed field change: what the album says now, and what the
/// database result says. Track-title changes carry the 1-based track number
/// in <see cref="Track"/>; album-level changes carry 0.</summary>
public sealed record EnrichmentChange(string Field, int Track, string Current, string Proposed)
{
    public string Display => Track > 0 ? $"Track {Track:00} {Field}" : Field;
}

/// <summary>The preview-diff for one album (D-048): every change the chosen
/// database release would make, shown before anything is written. Proposals
/// hold no open file handles; Apply reopens the album and writes only the
/// approved changes.</summary>
public sealed class EnrichmentProposal
{
    public string Source { get; init; } = "";
    public string Provider { get; init; } = "";
    public string InfoUrl { get; init; } = "";
    public IReadOnlyList<EnrichmentChange> Changes { get; init; } = Array.Empty<EnrichmentChange>();
    /// <summary>HTTPS URL of the proposed front cover (D-049), set only when
    /// the album has no artwork and the release offers one; the bytes are
    /// fetched at Apply time, never stored in the proposal.</summary>
    public string CoverUrl { get; init; } = "";
    public bool HasChanges => Changes.Count > 0;
}

public interface IEnrichmentService
{
    /// <summary>Look the album up and diff the best database release against
    /// its current metadata. Null when no database release was found. Offline
    /// (both database endpoints unreachable), the request journals as an
    /// enrichment-pending entry (D-010's second lane) and
    /// <see cref="EnrichmentOfflineException"/> is thrown so the caller can
    /// say so honestly.</summary>
    EnrichmentProposal? Propose(string path);

    /// <summary>Write the approved changes into the audio files' tags. The
    /// source .cue text is not rewritten (increment A debt). Returns the
    /// number of files whose tags changed.</summary>
    int Apply(EnrichmentProposal proposal);
}

/// <summary>
/// Tag enrichment (SLICE-008 increment A). Propose runs the engine's own
/// album lookup (CTDB metadata search per the config's mode) and produces an
/// honest field diff; Apply writes ONLY the approved fields through TagLib
/// into per-track files. Image-layout albums (one file + cue) receive the
/// album-level fields only; their track titles live in the cue text, which
/// this increment does not rewrite.
/// </summary>
/// <summary>Thrown when an enrichment lookup could not run because the
/// databases are unreachable; the request has been journaled.</summary>
public sealed class EnrichmentOfflineException : Exception
{
    public EnrichmentOfflineException()
        : base("The databases are unreachable. The lookup was journaled and " +
               "will be offered again on an online launch.") { }
}

public sealed class EnrichmentService : IEnrichmentService
{
    private readonly CUEConfig _config;
    private readonly IDiagnosticLog _log;
    private readonly Journal.JournalStore? _journal;
    private readonly Func<bool> _isOnline;
    private readonly Func<string, byte[]> _fetchBytes;

    public EnrichmentService(
        CUEConfig config,
        IDiagnosticLog log,
        Journal.JournalStore? journal = null,
        Func<bool>? isOnline = null,
        Func<string, byte[]>? fetchBytes = null)
    {
        _config = config;
        _log = log;
        _journal = journal;
        _isOnline = isOnline ?? Journal.ConnectivityProbe.IsOnline;
        _fetchBytes = fetchBytes ?? FetchHttps;
    }

    private const int MaxCoverDownloadBytes = 20 * 1024 * 1024;

    private static byte[] FetchHttps(string url)
    {
        var uri = new Uri(url, UriKind.Absolute);
        if (uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Cover downloads are HTTPS-only.");
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        using HttpResponseMessage response = client
            .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead)
            .GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > MaxCoverDownloadBytes)
            throw new InvalidOperationException("Cover image is implausibly large.");
        using var stream = response.Content.ReadAsStream();
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer, 81920);
        if (buffer.Length > MaxCoverDownloadBytes)
            throw new InvalidOperationException("Cover image is implausibly large.");
        return buffer.ToArray();
    }

    public EnrichmentProposal? Propose(string path)
    {
        if (_journal != null && !_isOnline())
        {
            // Same double-check discipline as the verify lane: journal only
            // when the direct endpoint probe says offline, so a server error
            // does not masquerade as being offline.
            bool alreadyPending = _journal.ReadPending(Journal.BackfillLane.Enrichment)
                .Any(entry => string.Equals(entry.SourcePath, path, StringComparison.Ordinal));
            if (!alreadyPending)
            {
                _journal.CreatePending(Journal.BackfillLane.Enrichment, path, "");
                _log.Info("enrich", "offline: enrichment lookup journaled");
            }
            throw new EnrichmentOfflineException();
        }

        var cue = new CUESheet(_config);
        try
        {
            cue.Open(path);
            _log.Redact(cue.Metadata?.Artist, cue.Metadata?.Title);
            List<object> releases = cue.LookupAlbumInfo(
                useCache: false,
                useCUE: true,
                useCTDB: true,
                _config.advanced.metadataSearch);
            // "local", "cue", and "tags" echo the album's own current
            // metadata back as pseudo-releases; a database proposal must come
            // from a real provider or it just matches itself.
            var selfKeys = new[] { "local", "cue", "tags" };
            CUEMetadataEntry? best = releases
                .OfType<CUEMetadataEntry>()
                .FirstOrDefault(entry =>
                    !selfKeys.Contains(entry.ImageKey) &&
                    !string.IsNullOrWhiteSpace(entry.metadata?.Artist) &&
                    !string.IsNullOrWhiteSpace(entry.metadata?.Title));
            if (best == null) return null;

            CUEMetadata current = cue.Metadata ?? new CUEMetadata(cue.TOC.TOCID, (int)cue.TOC.AudioTracks);
            CUEMetadata proposed = best.metadata;
            // The diff basis is the album's EFFECTIVE metadata: the audio
            // files' tags override the cue text where set, so an applied
            // proposal is not proposed again forever (the cue text is not
            // rewritten in this increment).
            OverlayFileTags(cue, current);
            _log.Redact(proposed.Artist, proposed.Title);
            var changes = new List<EnrichmentChange>();
            void AlbumField(string name, string? cur, string? prop)
            {
                string c = cur ?? "", p = prop ?? "";
                if (p.Length > 0 && !string.Equals(c, p, StringComparison.Ordinal))
                    changes.Add(new EnrichmentChange(name, 0, c, p));
            }
            AlbumField("Artist", current.Artist, proposed.Artist);
            AlbumField("Album", current.Title, proposed.Title);
            AlbumField("Year", current.Year, proposed.Year);
            AlbumField("Genre", current.Genre, proposed.Genre);

            if (cue.HasTrackFilenames &&
                proposed.Tracks != null && current.Tracks != null)
            {
                int tracks = Math.Min(proposed.Tracks.Count, current.Tracks.Count);
                for (int i = 0; i < tracks; i++)
                {
                    string c = current.Tracks[i].Title ?? "";
                    string p = proposed.Tracks[i].Title ?? "";
                    if (p.Length > 0 && !string.Equals(c, p, StringComparison.Ordinal))
                        changes.Add(new EnrichmentChange("Title", i + 1, c, p));
                }
            }

            string coverUrl = "";
            if (!HasExistingArtwork(cue, path))
            {
                string approvedUri = "";
                var candidates = (proposed.AlbumArt ?? new())
                    .OrderByDescending(image => image.primary);
                foreach (var image in candidates)
                {
                    if (TryApprovedCoverUri(image.uri, out approvedUri)) break;
                }
                if (approvedUri.Length > 0)
                {
                    coverUrl = approvedUri;
                    changes.Add(new EnrichmentChange(
                        "Front cover", 0, "(none)", "cover image from the release"));
                }
            }

            return new EnrichmentProposal
            {
                Source = path,
                Provider = best.ImageKey ?? "",
                InfoUrl = best.InfoUrl ?? "",
                Changes = changes,
                CoverUrl = coverUrl,
            };
        }
        finally
        {
            cue.Close();
        }
    }

    public int Apply(EnrichmentProposal proposal)
    {
        var cue = new CUESheet(_config);
        List<string> sources;
        bool perTrack;
        try
        {
            cue.Open(proposal.Source);
            sources = new List<string>(cue.SourcePaths);
            perTrack = cue.HasTrackFilenames;
        }
        finally
        {
            cue.Close();
        }

        var albumChanges = proposal.Changes.Where(change => change.Track == 0).ToList();
        var titleByTrack = proposal.Changes
            .Where(change => change.Track > 0)
            .ToDictionary(change => change.Track, change => change.Proposed);

        int written = 0;
        for (int i = 0; i < sources.Count; i++)
        {
            using TagLib.File file = TagLib.File.Create(sources[i]);
            bool dirty = false;
            foreach (EnrichmentChange change in albumChanges)
            {
                switch (change.Field)
                {
                    case "Artist":
                        file.Tag.AlbumArtists = new[] { change.Proposed };
                        file.Tag.Performers = new[] { change.Proposed };
                        dirty = true;
                        break;
                    case "Album":
                        file.Tag.Album = change.Proposed;
                        dirty = true;
                        break;
                    case "Year":
                        if (uint.TryParse(change.Proposed, out uint year))
                        {
                            file.Tag.Year = year;
                            dirty = true;
                        }
                        break;
                    case "Genre":
                        file.Tag.Genres = new[] { change.Proposed };
                        dirty = true;
                        break;
                }
            }
            if (perTrack && titleByTrack.TryGetValue(i + 1, out string? title))
            {
                file.Tag.Title = title;
                file.Tag.Track = (uint)(i + 1);
                dirty = true;
            }
            if (dirty)
            {
                file.Save();
                written++;
            }
        }

        bool coverApproved = proposal.CoverUrl.Length > 0 &&
            proposal.Changes.Any(change => change.Field == "Front cover");
        if (coverApproved)
        {
            byte[] cover = PrepareCover(_fetchBytes(proposal.CoverUrl));
            var picture = new TagLib.Picture(new TagLib.ByteVector(cover))
            {
                Type = TagLib.PictureType.FrontCover,
                MimeType = "image/jpeg",
                Description = "",
            };
            foreach (string source in sources)
            {
                using TagLib.File file = TagLib.File.Create(source);
                file.Tag.Pictures = new TagLib.IPicture[] { picture };
                file.Save();
            }
            string folderJpg = Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(proposal.Source))!, "folder.jpg");
            if (!File.Exists(folderJpg))
            {
                using var stream = new FileStream(folderJpg, FileMode.CreateNew, FileAccess.Write);
                stream.Write(cover, 0, cover.Length);
            }
            written = Math.Max(written, sources.Count);
        }

        _log.Info("enrich", $"applied {proposal.Changes.Count} change(s) across {written} file(s)");
        return written;
    }

    /// <summary>The WPF artwork policy, mirrored: http upgrades to https,
    /// only default-port HTTPS to the cover-art hosts is accepted, and
    /// local/private addresses are rejected.</summary>
    private static bool TryApprovedCoverUri(string? value, out string approved)
    {
        approved = "";
        if (!Uri.TryCreate(value ?? "", UriKind.Absolute, out Uri? parsed)) return false;
        if (parsed.Scheme == Uri.UriSchemeHttp)
            parsed = new UriBuilder(parsed) { Scheme = Uri.UriSchemeHttps, Port = -1 }.Uri;
        if (parsed.Scheme != Uri.UriSchemeHttps || !parsed.IsDefaultPort) return false;
        string host = parsed.IdnHost;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            System.Net.IPAddress.TryParse(host, out _))
            return false;
        bool allowed =
            host.Equals("coverartarchive.org", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("archive.org", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".archive.org", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("db.cuetools.net", StringComparison.OrdinalIgnoreCase);
        if (!allowed) return false;
        approved = parsed.AbsoluteUri;
        return true;
    }

    private static void OverlayFileTags(CUESheet cue, CUEMetadata current)
    {
        try
        {
            var sources = cue.SourcePaths;
            if (sources.Count == 0) return;
            using (TagLib.File first = TagLib.File.Create(sources[0]))
            {
                if (!string.IsNullOrWhiteSpace(first.Tag.FirstAlbumArtist))
                    current.Artist = first.Tag.FirstAlbumArtist;
                if (!string.IsNullOrWhiteSpace(first.Tag.Album))
                    current.Title = first.Tag.Album;
                if (first.Tag.Year > 0)
                    current.Year = first.Tag.Year.ToString();
                if (!string.IsNullOrWhiteSpace(first.Tag.FirstGenre))
                    current.Genre = first.Tag.FirstGenre;
            }
            if (cue.HasTrackFilenames && current.Tracks != null)
            {
                int tracks = Math.Min(sources.Count, current.Tracks.Count);
                for (int i = 0; i < tracks; i++)
                {
                    using TagLib.File file = TagLib.File.Create(sources[i]);
                    if (!string.IsNullOrWhiteSpace(file.Tag.Title))
                        current.Tracks[i].Title = file.Tag.Title;
                }
            }
        }
        catch
        {
            // unreadable tags leave the cue-derived metadata as the basis
        }
    }

    private static bool HasExistingArtwork(CUESheet cue, string path)
    {
        if (cue.AlbumArt != null && cue.AlbumArt.Count > 0) return true;
        string? dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (dir == null) return false;
        return File.Exists(Path.Combine(dir, "folder.jpg")) ||
               File.Exists(Path.Combine(dir, "cover.jpg")) ||
               File.Exists(Path.Combine(dir, "Folder.jpg"));
    }

    /// <summary>Decode, cap to the configured maximum edge (D-049; the same
    /// maxAlbumArtSize the engine uses), and re-encode as JPEG. A JPEG that
    /// is already within the cap keeps its original bytes untranscoded.</summary>
    internal byte[] PrepareCover(byte[] bytes)
    {
        using var codec = SkiaSharp.SKCodec.Create(new MemoryStream(bytes));
        if (codec == null)
            throw new InvalidOperationException("The cover image could not be decoded.");
        int cap = Math.Max(256, _config.maxAlbumArtSize);
        var info = codec.Info;
        bool isJpeg = codec.EncodedFormat == SkiaSharp.SKEncodedImageFormat.Jpeg;
        if (isJpeg && info.Width <= cap && info.Height <= cap)
            return bytes;

        using var original = SkiaSharp.SKBitmap.Decode(bytes);
        if (original == null)
            throw new InvalidOperationException("The cover image could not be decoded.");
        SkiaSharp.SKBitmap final = original;
        SkiaSharp.SKBitmap? resized = null;
        try
        {
            if (original.Width > cap || original.Height > cap)
            {
                double scale = Math.Min((double)cap / original.Width, (double)cap / original.Height);
                var size = new SkiaSharp.SKImageInfo(
                    Math.Max(1, (int)(original.Width * scale)),
                    Math.Max(1, (int)(original.Height * scale)));
                resized = original.Resize(size, SkiaSharp.SKSamplingOptions.Default);
                if (resized != null) final = resized;
            }
            using var image = SkiaSharp.SKImage.FromBitmap(final);
            using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 90);
            return data.ToArray();
        }
        finally
        {
            resized?.Dispose();
        }
    }
}
