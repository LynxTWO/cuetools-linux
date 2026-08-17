using Avalonia.Headless.XUnit;
using CUETools.Linux.App;
using CUETools.Processor;
using CUETools.Wpf.Models;
using CUETools.Wpf.Services;
using CUETools.Wpf.ViewModels;
using Xunit;

namespace CUETools.Linux.Tests;

// SLICE-004 queue-flow wiring: S4-002 (a queued conversion pins its codec by
// stable id and a not-ready codec fails that item plainly while the batch
// continues), S4-003 (verify items use the Verify page's verdict vocabulary),
// S4-004 (Remove/Clear blocked while running; finished item state is never
// erased by a later item).
//
// AvaloniaFact, not Fact: RelayCommands register on the static RequeryHub for
// the process lifetime (see AutoRepairDriverTests for the full story).
public class QueueFlowTests
{
    private sealed class ScriptedVerifyService : IVerifyService
    {
        public Func<string, VerifyFilesResult> OnVerify = path => new()
        {
            Ok = true,
            Status = "Verified.",
            ArConfidence = 5,
            Accurate = true,
            Source = path,
        };

        public VerifyFilesResult Verify(string path, Action<double, string> progress)
            => OnVerify(path);

        public VerifyFilesResult Repair(string path, Action<double, string> progress)
            => throw new NotSupportedException();
    }

    private sealed class ScriptedConvertService : IConvertService
    {
        public Func<string, ConvertResult> OnConvert = _ => new()
        {
            Ok = true, Status = "done", FileCount = 1, OutputDir = "/tmp/x",
        };
        public int Calls;

        public IReadOnlyList<string> LosslessFormats() => new[] { "flac", "wav", "m4a" };
        public IReadOnlyList<string> LossyFormats() => Array.Empty<string>();
        public bool IsLossy(string format) => false;
        public SourcePreview PreloadSource(string path) => new();

        public ConvertResult Convert(string path, string format, string outputDir,
            Action<double, string> onProgress)
        {
            Calls++;
            return OnConvert(path);
        }
    }

    private sealed class NullLog : IDiagnosticLog
    {
        public string LogPath => "";
        public void Info(string category, string message) { }
        public void Warn(string category, string message) { }
        public void Error(string category, string message, Exception? ex = null) { }
        public void Redact(params string?[] sensitive) { }
    }

    private static string WriteAlbumDir()
    {
        string dir = Path.Combine(
            Path.GetTempPath(), $"cuetools-queue-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "album.cue"),
            "FILE \"track01.wav\" WAVE\n  TRACK 01 AUDIO\n    INDEX 01 00:00:00\n");
        return dir;
    }

    private static (QueueViewModel vm, ScriptedVerifyService verify, ScriptedConvertService convert, CUEConfig config, EncoderCatalog catalog)
        CreateViewModel()
    {
        Composition.RegisterManagedCodecs();
        CUEConfig config = Composition.CreateDefaultConfig();
        var catalog = new EncoderCatalog(new NullLog(), new AppSettings());
        var verify = new ScriptedVerifyService();
        var convert = new ScriptedConvertService();
        var viewModel = new QueueViewModel(verify, convert, catalog, config);
        return (viewModel, verify, convert, config, catalog);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (int i = 0; i < 200 && !condition(); i++)
            await Task.Delay(25, TestContext.Current.CancellationToken);
        Assert.True(condition(), "timed out waiting for queue state");
    }

    [AvaloniaFact]
    public async Task VerifyItemsUseTheVerifyVerdictVocabulary()
    {
        var (vm, verify, _, _, _) = CreateViewModel();
        string a = WriteAlbumDir(), b = WriteAlbumDir(), c = WriteAlbumDir();
        try
        {
            verify.OnVerify = path => path.StartsWith(a, StringComparison.Ordinal)
                ? new VerifyFilesResult { Ok = true, Status = "ok", Accurate = true, ArConfidence = 3, Source = path }
                : path.StartsWith(b, StringComparison.Ordinal)
                    ? new VerifyFilesResult { Ok = true, Status = "damaged", HasErrors = true, CanRecover = true, Source = path }
                    : new VerifyFilesResult { Ok = true, Status = "no match", Source = path };

            Assert.True(vm.EnqueuePath(a));
            Assert.True(vm.EnqueuePath(b));
            Assert.True(vm.EnqueuePath(c));
            vm.RunAllCommand.Execute(null);
            await WaitUntilAsync(() => !vm.IsRunning && vm.Items.All(i => i.Status != "Pending" && i.Status != "Running"));

            Assert.Equal("Verified", vm.Items[0].Status);
            Assert.Equal("Repairable", vm.Items[1].Status);
            Assert.Equal("No match", vm.Items[2].Status);
        }
        finally
        {
            Directory.Delete(a, true); Directory.Delete(b, true); Directory.Delete(c, true);
        }
    }

    [AvaloniaFact]
    public async Task NotReadyQueuedCodecFailsThatItemAndTheBatchContinues()
    {
        var (vm, _, convert, _, _) = CreateViewModel();
        string a = WriteAlbumDir(), b = WriteAlbumDir();
        try
        {
            vm.SelectedAction = "Convert";
            vm.SelectedFormat = "flac";
            Assert.True(vm.EnqueuePath(a));
            // Corrupt the first item's pinned codec id to something that can
            // never resolve; the second item keeps the real selection.
            QueueItem first = vm.Items[0];
            vm.Items[0] = new QueueItem
            {
                Source = first.Source,
                Action = first.Action,
                Format = first.Format,
                CodecStableId = "no-such-codec",
            };
            Assert.True(vm.EnqueuePath(b));

            vm.RunAllCommand.Execute(null);
            await WaitUntilAsync(() => !vm.IsRunning && vm.Items.All(i => i.Status != "Pending" && i.Status != "Running"));

            Assert.Equal("Failed", vm.Items[0].Status);
            Assert.Contains("no longer ready", vm.Items[0].Result);
            Assert.Equal("Done", vm.Items[1].Status);
            Assert.Equal(1, convert.Calls);
        }
        finally
        {
            Directory.Delete(a, true); Directory.Delete(b, true);
        }
    }

    [AvaloniaFact]
    public async Task ClearIsBlockedWhileRunningAndFinishedStateSurvives()
    {
        var (vm, verify, _, _, _) = CreateViewModel();
        string a = WriteAlbumDir(), b = WriteAlbumDir();
        try
        {
            // Hold the FIRST item, not the second, and wait on that item's own Running
            // flag rather than on IsRunning. IsRunning goes true when the batch starts,
            // which says nothing about which row is in flight: holding the second item
            // let the first one finish before the asserts ran, so
            // RemoveCommand.CanExecute(Items[0]) was a race. It passed alone and in a
            // quiet suite, and failed once under the load of a concurrent AOT compile.
            var gate = new SemaphoreSlim(0);
            int calls = 0;
            verify.OnVerify = path =>
            {
                if (Interlocked.Increment(ref calls) == 1) gate.Wait(TimeSpan.FromSeconds(5));
                return new VerifyFilesResult { Ok = true, Status = "ok", Accurate = true, ArConfidence = 1, Source = path };
            };

            Assert.True(vm.EnqueuePath(a));
            Assert.True(vm.EnqueuePath(b));
            vm.RunAllCommand.Execute(null);
            await WaitUntilAsync(() => vm.Items[0].Running);

            Assert.True(vm.IsRunning);
            Assert.False(vm.ClearCommand.CanExecute(null));
            // The row that is running cannot be removed, because the job would have no
            // row to report into. A row still waiting can be, which is what lets a user
            // drop a mistaken entry without clearing the whole batch.
            Assert.False(vm.RemoveCommand.CanExecute(vm.Items[0]));
            Assert.True(vm.RemoveCommand.CanExecute(vm.Items[1]));

            gate.Release();
            await WaitUntilAsync(() => !vm.IsRunning);

            // finished state survives the batch end
            Assert.All(vm.Items, item => Assert.Equal("Verified", item.Status));
            Assert.True(vm.ClearCommand.CanExecute(null));
        }
        finally
        {
            Directory.Delete(a, true); Directory.Delete(b, true);
        }
    }
}
