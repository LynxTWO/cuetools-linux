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
    public bool HasChanges => Changes.Count > 0;
}

public interface IEnrichmentService
{
    /// <summary>Look the album up and diff the best database release against
    /// its current metadata. Null when no database release was found.</summary>
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
public sealed class EnrichmentService : IEnrichmentService
{
    private readonly CUEConfig _config;
    private readonly IDiagnosticLog _log;

    public EnrichmentService(CUEConfig config, IDiagnosticLog log)
    {
        _config = config;
        _log = log;
    }

    public EnrichmentProposal? Propose(string path)
    {
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
            CUEMetadataEntry? best = releases
                .OfType<CUEMetadataEntry>()
                .FirstOrDefault(entry =>
                    entry.ImageKey != "local" &&
                    !string.IsNullOrWhiteSpace(entry.metadata?.Artist) &&
                    !string.IsNullOrWhiteSpace(entry.metadata?.Title));
            if (best == null) return null;

            CUEMetadata current = cue.Metadata ?? new CUEMetadata(cue.TOC.TOCID, (int)cue.TOC.AudioTracks);
            CUEMetadata proposed = best.metadata;
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

            return new EnrichmentProposal
            {
                Source = path,
                Provider = best.ImageKey ?? "",
                InfoUrl = best.InfoUrl ?? "",
                Changes = changes,
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

        _log.Info("enrich", $"applied {proposal.Changes.Count} change(s) across {written} file(s)");
        return written;
    }
}
