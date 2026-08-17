using Avalonia.Headless.XUnit;
using CUETools.Linux.App;
using CUETools.Processor;
using CUETools.Wpf.Models;
using CUETools.Wpf.Services;
using CUETools.Wpf.ViewModels;
using Xunit;

namespace CUETools.Linux.Tests;

// F-08: Convert and Queue offered a folder button whose path the engine rejects with
// "is a directory" (CUESheet.Open), so it failed every time it was pressed. Both now
// resolve a folder to the manifests inside it, the way the Verify page always has.
//
// AvaloniaFact for the view-model tests, per the RequeryHub story in AutoRepairDriverTests.
public class FolderInputResolutionTests
{
    private sealed class NoVerify : IVerifyService
    {
        public VerifyFilesResult Verify(string path, Action<double, string> progress)
            => new() { Source = path, Error = "not run" };

        public VerifyFilesResult Repair(string path, Action<double, string> progress)
            => throw new NotSupportedException();
    }

    private sealed class NoConvert : IConvertService
    {
        public IReadOnlyList<string> LosslessFormats() => new[] { "flac", "wav" };
        public IReadOnlyList<string> LossyFormats() => Array.Empty<string>();
        public bool IsLossy(string format) => false;
        public SourcePreview PreloadSource(string path) => new();

        public ConvertResult Convert(string path, string format, string outputDir,
            Action<double, string> onProgress)
            => new() { Ok = false, Error = "not run" };
    }

    private sealed class NullLog : IDiagnosticLog
    {
        public string LogPath => "";
        public void Info(string category, string message) { }
        public void Warn(string category, string message) { }
        public void Error(string category, string message, Exception? ex = null) { }
        public void Redact(params string?[] sensitive) { }
    }

    private static string NewDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"folder-input-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void WriteAlbum(string dir, string cue, string audio, int disc, int total)
    {
        File.WriteAllBytes(Path.Combine(dir, audio), new byte[] { 1 });
        File.WriteAllText(
            Path.Combine(dir, cue),
            (disc > 0 ? $"REM DISCNUMBER {disc}\nREM TOTALDISCS {total}\n" : "") +
            $"FILE \"{audio}\" WAVE\n  TRACK 01 AUDIO\n    INDEX 01 00:00:00\n");
    }

    private static QueueViewModel NewQueue()
    {
        Composition.RegisterManagedCodecs();
        return new QueueViewModel(
            new NoVerify(),
            new NoConvert(),
            new EncoderCatalog(new NullLog(), new AppSettings()),
            Composition.CreateDefaultConfig());
    }

    [AvaloniaFact]
    public void QueueingAFolderQueuesItsManifestRatherThanTheDirectory()
    {
        string dir = NewDir();
        try
        {
            WriteAlbum(dir, "album.cue", "01.wav", 0, 0);

            QueueViewModel queue = NewQueue();
            Assert.True(queue.EnqueuePath(dir));

            string source = Assert.Single(queue.Items).Source;
            Assert.EndsWith("album.cue", source);
            Assert.False(Directory.Exists(source),
                "a directory is exactly what the engine refuses to open");
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [AvaloniaFact]
    public void AMultiDiscFolderBecomesOneQueueItemPerDisc()
    {
        string dir = NewDir();
        try
        {
            string cd1 = Path.Combine(dir, "CD1");
            string cd2 = Path.Combine(dir, "CD2");
            Directory.CreateDirectory(cd1);
            Directory.CreateDirectory(cd2);
            WriteAlbum(cd1, "disc1.cue", "01.wav", 1, 2);
            WriteAlbum(cd2, "disc2.cue", "02.wav", 2, 2);

            QueueViewModel queue = NewQueue();
            Assert.True(queue.EnqueuePath(dir));

            Assert.Equal(2, queue.Items.Count);
            foreach (var item in queue.Items)
                Assert.False(Directory.Exists(item.Source));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [AvaloniaFact]
    public void AFolderWithNothingUsableIsNotQueued()
    {
        string dir = NewDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "notes.txt"), "nothing here");

            QueueViewModel queue = NewQueue();

            Assert.False(queue.EnqueuePath(dir));
            Assert.Empty(queue.Items);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void DiscoveryResolvesAFolderToItsCueSheet()
    {
        string dir = NewDir();
        try
        {
            WriteAlbum(dir, "album.cue", "01.wav", 0, 0);

            VerificationSourceDiscoveryResult found =
                new VerificationSourceDiscovery().Discover(new[] { dir });

            Assert.True(found.Ok, found.Error);
            Assert.EndsWith("album.cue", found.SourceSet!.Discs.Single().Path);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }
}
