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
