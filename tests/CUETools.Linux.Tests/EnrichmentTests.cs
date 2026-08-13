using CUETools.Linux.App;
using CUETools.Linux.App.Services;
using CUETools.Wpf.Services;
using Xunit;

namespace CUETools.Linux.Tests;

// SLICE-008 increment A: Apply writes exactly the approved fields into the
// audio files' tags and nothing else (S8-002). Propose needs the live
// databases and is covered by the evidence run, not unit tests.
public class EnrichmentTests
{
    private sealed class NullLog : IDiagnosticLog
    {
        public string LogPath => "";
        public void Info(string category, string message) { }
        public void Warn(string category, string message) { }
        public void Error(string category, string message, Exception? ex = null) { }
        public void Redact(params string?[] sensitive) { }
    }

    private static string WriteFlacAlbum(int tracks)
    {
        Composition.RegisterManagedCodecs();
        string dir = Path.Combine(
            Path.GetTempPath(), $"cuetools-enrich-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var pcm = new CUETools.Codecs.AudioPCMConfig(16, 2, 44100);
        var cue = new System.Text.StringBuilder();
        for (int i = 1; i <= tracks; i++)
        {
            string name = $"track{i:00}.flac";
            var settings = new CUETools.Codecs.Flake.EncoderSettings { PCM = pcm };
            var encoder = new CUETools.Codecs.Flake.AudioEncoder(
                settings, Path.Combine(dir, name));
            encoder.Write(new CUETools.Codecs.AudioBuffer(pcm, new int[11025, 2], 11025));
            encoder.Close();
            cue.AppendLine($"FILE \"{name}\" WAVE");
            cue.AppendLine($"  TRACK {i:00} AUDIO");
            cue.AppendLine($"    INDEX 01 00:00:00");
        }
        File.WriteAllText(Path.Combine(dir, "album.cue"), cue.ToString());
        return dir;
    }

    [Fact]
    public void ApplyWritesExactlyTheApprovedFields()
    {
        string dir = WriteFlacAlbum(2);
        try
        {
            var service = new EnrichmentService(Composition.CreateDefaultConfig(), new NullLog());
            var proposal = new EnrichmentProposal
            {
                Source = Path.Combine(dir, "album.cue"),
                Provider = "test",
                Changes = new[]
                {
                    new EnrichmentChange("Artist", 0, "", "Real Artist"),
                    new EnrichmentChange("Album", 0, "", "Real Album"),
                    new EnrichmentChange("Year", 0, "", "1999"),
                    new EnrichmentChange("Title", 1, "", "First Song"),
                    new EnrichmentChange("Title", 2, "", "Second Song"),
                },
            };
            int written = service.Apply(proposal);
            Assert.Equal(2, written);

            using (var one = TagLib.File.Create(Path.Combine(dir, "track01.flac")))
            {
                Assert.Equal("Real Artist", one.Tag.FirstAlbumArtist);
                Assert.Equal("Real Album", one.Tag.Album);
                Assert.Equal(1999u, one.Tag.Year);
                Assert.Equal("First Song", one.Tag.Title);
                Assert.Empty(one.Tag.Genres); // unapproved field untouched
            }
            using (var two = TagLib.File.Create(Path.Combine(dir, "track02.flac")))
            {
                Assert.Equal("Second Song", two.Tag.Title);
            }
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void OfflineProposeJournalsOnceAndSaysSo()
    {
        string journalDir = Path.Combine(
            Path.GetTempPath(), $"cuetools-enrich-journal-{Guid.NewGuid():N}");
        string dir = WriteFlacAlbum(1);
        try
        {
            var journal = new CUETools.Linux.App.Journal.JournalStore(journalDir);
            var service = new EnrichmentService(
                Composition.CreateDefaultConfig(), new NullLog(),
                journal, isOnline: () => false);
            string cue = Path.Combine(dir, "album.cue");

            Assert.Throws<EnrichmentOfflineException>(() => service.Propose(cue));
            Assert.Throws<EnrichmentOfflineException>(() => service.Propose(cue));

            var pending = journal.ReadPending(
                CUETools.Linux.App.Journal.BackfillLane.Enrichment);
            Assert.Single(pending);   // idempotent: one entry per album
            Assert.Equal(cue, pending[0].SourcePath);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
            Directory.Delete(journalDir, recursive: true);
        }
    }

    private static byte[] MakePng(int width, int height)
    {
        using var bitmap = new SkiaSharp.SKBitmap(width, height);
        using (var canvas = new SkiaSharp.SKCanvas(bitmap))
            canvas.Clear(new SkiaSharp.SKColor(30, 160, 140));
        using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    [Fact]
    public void PrepareCoverCapsOversizedImagesAndKeepsSmallJpegs()
    {
        var service = new EnrichmentService(Composition.CreateDefaultConfig(), new NullLog());

        byte[] big = service.PrepareCover(MakePng(4000, 2000));
        using (var codec = SkiaSharp.SKCodec.Create(new MemoryStream(big)))
        {
            Assert.Equal(SkiaSharp.SKEncodedImageFormat.Jpeg, codec!.EncodedFormat);
            Assert.True(codec.Info.Width <= 1500 && codec.Info.Height <= 1500);
            Assert.Equal(2.0, (double)codec.Info.Width / codec.Info.Height, 1);
        }

        byte[] smallJpeg;
        using (var bitmap = SkiaSharp.SKBitmap.Decode(MakePng(600, 600)))
        using (var image = SkiaSharp.SKImage.FromBitmap(bitmap))
        using (var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 90))
            smallJpeg = data.ToArray();
        Assert.Same(smallJpeg, service.PrepareCover(smallJpeg)); // untranscoded
    }

    [Fact]
    public void ApplyEmbedsApprovedCoverAndWritesFolderJpgOnce()
    {
        string dir = WriteFlacAlbum(2);
        try
        {
            byte[] png = MakePng(800, 800);
            var service = new EnrichmentService(
                Composition.CreateDefaultConfig(), new NullLog(),
                fetchBytes: _ => png);
            var proposal = new EnrichmentProposal
            {
                Source = Path.Combine(dir, "album.cue"),
                CoverUrl = "https://example.invalid/cover.png",
                Changes = new[]
                {
                    new EnrichmentChange("Front cover", 0, "(none)", "800x800 image from the release"),
                },
            };
            int written = service.Apply(proposal);
            Assert.Equal(2, written);

            using (var one = TagLib.File.Create(Path.Combine(dir, "track01.flac")))
            {
                Assert.Single(one.Tag.Pictures);
                Assert.Equal(TagLib.PictureType.FrontCover, one.Tag.Pictures[0].Type);
            }
            string folderJpg = Path.Combine(dir, "folder.jpg");
            Assert.True(File.Exists(folderJpg));

            // an existing folder.jpg is never overwritten
            byte[] before = File.ReadAllBytes(folderJpg);
            service.Apply(proposal);
            Assert.Equal(before, File.ReadAllBytes(folderJpg));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ApplyWithNoChangesWritesNothing()
    {
        string dir = WriteFlacAlbum(1);
        try
        {
            var service = new EnrichmentService(Composition.CreateDefaultConfig(), new NullLog());
            var before = File.GetLastWriteTimeUtc(Path.Combine(dir, "track01.flac"));
            int written = service.Apply(new EnrichmentProposal
            {
                Source = Path.Combine(dir, "album.cue"),
            });
            Assert.Equal(0, written);
            Assert.Equal(before, File.GetLastWriteTimeUtc(Path.Combine(dir, "track01.flac")));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
