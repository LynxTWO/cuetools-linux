using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using CUETools.Codecs;
using CUETools.Linux.App.Views;
using CUETools.Wpf.Services;
using CUETools.Wpf.ViewModels;
using Xunit;

namespace CUETools.Linux.Tests;

// SLICE-002 repair-flow wiring: S2-003 (no repair affordance without
// recoverable damage; declined confirmation runs nothing), S2-004 (failed
// repair keeps prior evidence and reports plainly), S2-005 (fail-closed
// without a prompt service). The engine-level preservation transaction is
// covered by the fork's own suite; these pin the app-side flow.
public class RepairFlowHeadlessTests
{
    private sealed class ScriptedService : IVerifyService
    {
        public VerifyFilesResult VerifyResult = new()
        {
            Ok = true,
            Status = "Verified.",
            HasErrors = true,
            CanRecover = true,
            TrackCount = 1,
            Tracks = new[] { new VerifyTrackResult { Number = 1, Title = "T", Crc32 = "AAAA" } },
        };

        public Func<string, VerifyFilesResult>? OnRepair;

        public int RepairCalls { get; private set; }

        public VerifyFilesResult Verify(string path, Action<double, string> progress)
            => VerifyResult;

        public VerifyFilesResult Repair(string path, Action<double, string> progress)
        {
            RepairCalls++;
            return OnRepair!(path);
        }
    }

    private sealed class ScriptedPrompt : IUserPrompt
    {
        public bool Answer;
        public int Asked;

        public Task<bool> ConfirmOkCancelAsync(string message, string title)
        {
            Asked++;
            return Task.FromResult(Answer);
        }
    }

    private static string WriteFixtureAlbum()
    {
        string dir = Path.Combine(
            Path.GetTempPath(), $"cuetools-repair-fixture-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var pcm = new AudioPCMConfig(16, 2, 44100);
        var encoder = new CUETools.Codecs.WAV.AudioEncoder(
            new CUETools.Codecs.WAV.EncoderSettings(pcm),
            Path.Combine(dir, "track01.wav"));
        encoder.Write(new AudioBuffer(pcm, new int[11025, 2], 11025));
        encoder.Close();
        File.WriteAllText(Path.Combine(dir, "album.cue"),
            "FILE \"track01.wav\" WAVE\n  TRACK 01 AUDIO\n    INDEX 01 00:00:00\n");
        return dir;
    }

    private static async Task<(VerifyViewModel vm, VerifyDiscViewModel disc, ScriptedService svc, ScriptedPrompt prompt, string dir)>
        VerifiedRepairableDiscAsync(bool promptAnswer, bool withPrompt = true)
    {
        string dir = WriteFixtureAlbum();
        var service = new ScriptedService();
        var prompt = new ScriptedPrompt { Answer = promptAnswer };
        var viewModel = new VerifyViewModel(
            service,
            new ReportStore(),
            new VerificationSourceDiscovery(),
            dialogs: null,
            prompts: withPrompt ? prompt : null,
            dispatcher: null);

        var window = new Window { Content = new VerifyView { DataContext = viewModel } };
        window.Show();

        Assert.True(viewModel.LoadSources(new[] { dir }));
        viewModel.VerifyCommand.Execute(null);
        await WaitUntilAsync(() => !viewModel.IsBusy && viewModel.HasResult);
        VerifyDiscViewModel disc = Assert.Single(viewModel.Discs);
        Assert.True(disc.CanRepair);
        return (viewModel, disc, service, prompt, dir);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (int i = 0; i < 200 && !condition(); i++)
            await Task.Delay(25);
        Assert.True(condition(), "timed out waiting for view-model state");
    }

    [AvaloniaFact]
    public async Task DecliningConfirmationRunsNoRepair()
    {
        var (vm, disc, svc, prompt, dir) = await VerifiedRepairableDiscAsync(promptAnswer: false);
        try
        {
            vm.RepairDiscCommand.Execute(disc);
            await WaitUntilAsync(() => prompt.Asked == 1 && !vm.IsBusy);
            Assert.Equal(0, svc.RepairCalls);
            Assert.True(disc.CanRepair);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task NoPromptServiceMeansNoRepair()
    {
        var (vm, disc, svc, _, dir) = await VerifiedRepairableDiscAsync(
            promptAnswer: true, withPrompt: false);
        try
        {
            vm.RepairDiscCommand.Execute(disc);
            await Task.Delay(500);
            Assert.False(vm.IsBusy);
            Assert.Equal(0, svc.RepairCalls);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task FailedRepairKeepsVerificationEvidenceAndReportsPlainly()
    {
        var (vm, disc, svc, _, dir) = await VerifiedRepairableDiscAsync(promptAnswer: true);
        try
        {
            svc.OnRepair = path => new VerifyFilesResult
            {
                Error = "parity fetch failed",
                Source = path,
            };
            vm.RepairDiscCommand.Execute(disc);
            await WaitUntilAsync(() => svc.RepairCalls == 1 && !vm.IsBusy);
            Assert.True(disc.HasResult);
            Assert.True(disc.CanRepair);
            Assert.Contains("Repair failed", disc.StatusText);
            Assert.Contains("evidence was kept", disc.StatusText);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task SuccessfulRepairFlipsCardToRepairedWithOutput()
    {
        var (vm, disc, svc, _, dir) = await VerifiedRepairableDiscAsync(promptAnswer: true);
        try
        {
            svc.OnRepair = path => new VerifyFilesResult
            {
                Ok = true,
                Status = "Repaired and verified.",
                RepairApplied = true,
                OutputPath = "/music/album (repaired)",
                TrackCount = 1,
                Tracks = new[] { new VerifyTrackResult { Number = 1, Title = "T", Crc32 = "BBBB" } },
                Source = path,
            };
            vm.RepairDiscCommand.Execute(disc);
            await WaitUntilAsync(() => disc.RepairApplied && !vm.IsBusy);
            Assert.False(disc.CanRepair);
            Assert.Equal("REPAIRED + VERIFIED", disc.OutcomeText);
            Assert.Equal("/music/album (repaired)", disc.RepairOutput);
            Assert.True(disc.HasRepairOutput);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
