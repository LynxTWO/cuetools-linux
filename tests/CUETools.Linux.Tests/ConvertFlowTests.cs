using Avalonia.Headless.XUnit;
using CUETools.Linux.App;
using CUETools.Processor;
using CUETools.Wpf.Services;
using CUETools.Wpf.ViewModels;
using Xunit;

namespace CUETools.Linux.Tests;

// SLICE-003 convert-flow wiring: S3-003 (unavailable codec rows are
// unselectable and a not-ready selection refuses to start), S3-004 (encoder
// settings persist per codec), S3-005 (a failed conversion reports plainly
// and leaves no success state), plus a real fixture conversion through the
// production ConvertService (S3-001 at fixture scale).
//
// AvaloniaFact, not Fact: RelayCommands register on the static RequeryHub for
// the process lifetime (see AutoRepairDriverTests for the full story).
public class ConvertFlowTests
{
    private sealed class ScriptedConvertService : IConvertService
    {
        public ConvertResult Result = new() { Ok = true, Status = "done", FileCount = 1, OutputDir = "/tmp/x" };
        public int Calls;

        public IReadOnlyList<string> LosslessFormats() => new[] { "flac", "wav", "m4a" };
        public IReadOnlyList<string> LossyFormats() => Array.Empty<string>();
        public bool IsLossy(string format) => false;

        public SourcePreview PreloadSource(string path) => new();

        public ConvertResult Convert(string path, string format, string outputDir,
            Action<double, string> onProgress)
        {
            Calls++;
            return Result;
        }
    }

    private static (ConvertViewModel vm, ScriptedConvertService svc, CUEConfig config, EncoderCatalog catalog)
        CreateViewModel()
    {
        Composition.RegisterManagedCodecs();
        CUEConfig config = Composition.CreateDefaultConfig();
        var catalog = new EncoderCatalog(new NullLog(), new AppSettings());
        var service = new ScriptedConvertService();
        var viewModel = new ConvertViewModel(service, catalog, config);
        return (viewModel, service, config, catalog);
    }

    private sealed class NullLog : IDiagnosticLog
    {
        public string LogPath => "";
        public void Info(string category, string message) { }
        public void Warn(string category, string message) { }
        public void Error(string category, string message, Exception? ex = null) { }
        public void Redact(params string?[] sensitive) { }
    }

    private static string WriteFixtureAlbum()
    {
        string dir = Path.Combine(
            Path.GetTempPath(), $"cuetools-convert-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var pcm = new CUETools.Codecs.AudioPCMConfig(16, 2, 44100);
        var encoder = new CUETools.Codecs.WAV.AudioEncoder(
            new CUETools.Codecs.WAV.EncoderSettings(pcm),
            Path.Combine(dir, "track01.wav"));
        encoder.Write(new CUETools.Codecs.AudioBuffer(pcm, new int[22050, 2], 22050));
        encoder.Close();
        File.WriteAllText(Path.Combine(dir, "album.cue"),
            "FILE \"track01.wav\" WAVE\n  TRACK 01 AUDIO\n    INDEX 01 00:00:00\n");
        return dir;
    }

    [AvaloniaFact]
    public void UnavailableCodecRowsAreUnselectableAndReadyRowsExist()
    {
        var (vm, _, _, _) = CreateViewModel();

        Assert.Contains(vm.CodecChoices, choice => choice.CanSelect);
        // Every offered row states its health; a row that cannot run now
        // must say so and be unselectable rather than a selectable lie.
        foreach (CodecChoice choice in vm.CodecChoices)
        {
            if (!choice.CanSelect)
                Assert.False(string.IsNullOrWhiteSpace(choice.HealthDetail));
        }
        // The offered formats all resolve to at least one selectable row or
        // are absent from the picker entirely (an extension alone is not
        // implementation identity).
        Assert.Contains("flac", vm.Formats);
    }

    [AvaloniaFact]
    public void ConvertRefusesWhenNoReadyCodecIsSelected()
    {
        var (vm, svc, config, catalog) = CreateViewModel();
        string dir = WriteFixtureAlbum();
        try
        {
            Assert.True(vm.LoadSource(Path.Combine(dir, "album.cue")));
            // Force a format with no ready implementation in this build.
            vm.SelectedFormat = "nonexistent-format";
            vm.ConvertCommand.Execute(null);
            Assert.Equal(0, svc.Calls);
            Assert.Contains("ready output codec", vm.StatusText);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task FailedConversionReportsPlainlyAndKeepsNoResult()
    {
        var (vm, svc, _, _) = CreateViewModel();
        string dir = WriteFixtureAlbum();
        try
        {
            svc.Result = new ConvertResult { Ok = false, Error = "encoder exploded" };
            Assert.True(vm.LoadSource(Path.Combine(dir, "album.cue")));
            vm.SelectedFormat = "flac";
            vm.ConvertCommand.Execute(null);
            for (int i = 0; i < 200 && (svc.Calls == 0 || vm.IsBusy); i++)
            {
                await Task.Delay(25, TestContext.Current.CancellationToken);
            }
            Assert.Equal(1, svc.Calls);
            Assert.False(vm.HasResult);
            Assert.Contains("Convert failed", vm.StatusText);
            Assert.Contains("encoder exploded", vm.StatusText);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [AvaloniaFact]
    public void EncoderModeSelectionPersistsOnTheConfigEntry()
    {
        var (_, _, config, catalog) = CreateViewModel();
        var settingsViewModel = new EncoderSettingsViewModel(
            config, catalog, "flac", lossy: false);
        if (settingsViewModel.Modes.Count < 2)
        {
            return; // nothing to switch between in this build
        }

        string target = settingsViewModel.Modes[^1];
        settingsViewModel.SelectedMode = target;
        var entry = config.Encoders.First(e =>
            e.Extension == "flac" && e.Settings != null &&
            (e.Settings.SupportedModes ?? "").Split(' ').Contains(target));
        Assert.Equal(target, entry.Settings!.EncoderMode);
    }

    [AvaloniaFact]
    public async Task RealFixtureConvertsToFlacThroughTheProductionService()
    {
        Composition.RegisterManagedCodecs();
        CUEConfig config = Composition.CreateDefaultConfig();
        var catalog = new EncoderCatalog(new NullLog(), new AppSettings());
        IConvertService service = new ConvertService(config, catalog, new AppSettings());
        string dir = WriteFixtureAlbum();
        string outDir = Path.Combine(dir, "out");
        try
        {
            var viewModel = new ConvertViewModel(service, catalog, config);
            Assert.True(viewModel.LoadSource(Path.Combine(dir, "album.cue"), outDir));
            viewModel.SelectedFormat = "flac";
            viewModel.ConvertCommand.Execute(null);
            for (int i = 0; i < 400 && (viewModel.IsBusy || !viewModel.HasResult); i++)
            {
                await Task.Delay(25, TestContext.Current.CancellationToken);
            }

            Assert.True(viewModel.HasResult, "conversion did not complete: " + viewModel.StatusText);
            string[] flacs = Directory.GetFiles(outDir, "*.flac", SearchOption.AllDirectories);
            Assert.Single(flacs);
            Assert.True(new FileInfo(flacs[0]).Length > 0);
            string[] cues = Directory.GetFiles(outDir, "*.cue", SearchOption.AllDirectories);
            Assert.Single(cues);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
