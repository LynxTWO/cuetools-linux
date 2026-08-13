using CUETools.Linux.App.Services;
using CUETools.Wpf.Services;
using CUETools.Wpf.ViewModels;
using Xunit;

namespace CUETools.Linux.Tests;

// The --repair flag's driver: after an idle-with-results transition, each
// repairable disc is repaired in turn until none remain; discs that were
// never repairable are untouched.
public class AutoRepairDriverTests
{
    private sealed class ScriptedService : IVerifyService
    {
        public int RepairCalls { get; private set; }

        public VerifyFilesResult Verify(string path, Action<double, string> progress)
            => new()
            {
                Ok = true,
                Status = "Verified.",
                HasErrors = true,
                CanRecover = true,
                TrackCount = 1,
                Tracks = new[] { new VerifyTrackResult { Number = 1, Title = "T", Crc32 = "AAAA" } },
                Source = path,
            };

        public VerifyFilesResult Repair(string path, Action<double, string> progress)
        {
            RepairCalls++;
            return new VerifyFilesResult
            {
                Ok = true,
                Status = "Repaired and verified.",
                RepairApplied = true,
                OutputPath = path + " (repaired)",
                TrackCount = 1,
                Tracks = new[] { new VerifyTrackResult { Number = 1, Title = "T", Crc32 = "BBBB" } },
                Source = path,
            };
        }
    }

    private sealed class FailingRepairService : IVerifyService
    {
        public int RepairCalls { get; private set; }

        public VerifyFilesResult Verify(string path, Action<double, string> progress)
            => new()
            {
                Ok = true,
                Status = "Verified.",
                HasErrors = true,
                CanRecover = true,
                TrackCount = 1,
                Tracks = new[] { new VerifyTrackResult { Number = 1, Title = "T", Crc32 = "AAAA" } },
                Source = path,
            };

        public VerifyFilesResult Repair(string path, Action<double, string> progress)
        {
            RepairCalls++;
            return new VerifyFilesResult
            {
                Ok = false,
                Error = "evidence sealing failed",
                Source = path,
            };
        }
    }

    private static string WriteFixtureAlbum()
    {
        string dir = Path.Combine(
            Path.GetTempPath(), $"cuetools-autorepair-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var pcm = new CUETools.Codecs.AudioPCMConfig(16, 2, 44100);
        var encoder = new CUETools.Codecs.WAV.AudioEncoder(
            new CUETools.Codecs.WAV.EncoderSettings(pcm),
            Path.Combine(dir, "track01.wav"));
        encoder.Write(new CUETools.Codecs.AudioBuffer(pcm, new int[11025, 2], 11025));
        encoder.Close();
        File.WriteAllText(Path.Combine(dir, "album.cue"),
            "FILE \"track01.wav\" WAVE\n  TRACK 01 AUDIO\n    INDEX 01 00:00:00\n");
        return dir;
    }

    [Fact]
    public async Task RepairsEveryRepairableDiscAfterVerify()
    {
        string dir = WriteFixtureAlbum();
        try
        {
            var service = new ScriptedService();
            var viewModel = new VerifyViewModel(
                service,
                new ReportStore(),
                new VerificationSourceDiscovery(),
                dialogs: null,
                prompts: new AutoConfirmPrompt(),
                dispatcher: null);
            var driver = new AutoRepairDriver(viewModel);

            Assert.True(viewModel.LoadSources(new[] { dir }));
            viewModel.VerifyCommand.Execute(null);

            for (int i = 0; i < 200; i++)
            {
                if (!viewModel.IsBusy && viewModel.HasResult &&
                    viewModel.Discs.All(d => !d.CanRepair))
                {
                    break;
                }
                await Task.Delay(25, TestContext.Current.CancellationToken);
            }

            Assert.Equal(1, service.RepairCalls);
            Assert.Equal(1, driver.RepairsStarted);
            Assert.True(viewModel.Discs.Single().RepairApplied);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task DoesNotRetryADiscWhoseRepairFailed()
    {
        string dir = WriteFixtureAlbum();
        try
        {
            var service = new FailingRepairService();
            var viewModel = new VerifyViewModel(
                service,
                new ReportStore(),
                new VerificationSourceDiscovery(),
                dialogs: null,
                prompts: new AutoConfirmPrompt(),
                dispatcher: null);
            var driver = new AutoRepairDriver(viewModel);

            Assert.True(viewModel.LoadSources(new[] { dir }));
            viewModel.VerifyCommand.Execute(null);

            // A failed repair leaves the disc repairable, so idle-with-results
            // recurs; the driver must not attempt the same disc again. Wait
            // past the first attempt, then hold long enough that a retry loop
            // (observed live before the guard) would have fired again.
            for (int i = 0; i < 200 && service.RepairCalls == 0; i++)
            {
                await Task.Delay(25, TestContext.Current.CancellationToken);
            }
            for (int i = 0; i < 40; i++)
            {
                await Task.Delay(25, TestContext.Current.CancellationToken);
                Assert.True(service.RepairCalls <= 1, "repair was retried after failing");
            }

            Assert.Equal(1, service.RepairCalls);
            Assert.Equal(1, driver.RepairsStarted);
            Assert.True(viewModel.Discs.Single().CanRepair);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
