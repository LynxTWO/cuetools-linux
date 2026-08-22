using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using CUETools.Codecs;
using CUETools.Linux.Tests;
using CUETools.Linux.App.Views;
using CUETools.Wpf.Services;
using CUETools.Wpf.ViewModels;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace CUETools.Linux.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure<CUETools.Linux.App.App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).UseSkia();
}

// Headless UI wiring tests: the Avalonia VerifyView instantiates against the
// shared app core's VerifyViewModel, discovery resolves a real fixture
// folder, and bindings materialize. Network services are faked; the fork's
// own 241-test suite covers the view-model behavior in depth.
public class VerifyViewHeadlessTests
{
    private sealed class FakeVerifyService : IVerifyService
    {
        public VerifyFilesResult Verify(string path, Action<double, string> progress)
            => new() { Source = path, Error = "fake: not run" };

        public VerifyFilesResult Repair(string path, Action<double, string> progress)
            => new() { Source = path, Error = "fake: not run" };
    }

    private static string WriteFixtureAlbum()
    {
        string dir = Path.Combine(
            Path.GetTempPath(), $"cuetools-linux-ui-fixture-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var pcm = new AudioPCMConfig(16, 2, 44100);
        int total = 44100 / 4;
        var samples = new int[total, 2];
        var encoder = new CUETools.Codecs.WAV.AudioEncoder(
            new CUETools.Codecs.WAV.EncoderSettings(pcm),
            Path.Combine(dir, "track01.wav"));
        encoder.Write(new AudioBuffer(pcm, samples, total));
        encoder.Close();
        File.WriteAllText(Path.Combine(dir, "album.cue"),
            "PERFORMER \"Fixture\"\n" +
            "TITLE \"Headless Album\"\n" +
            "FILE \"track01.wav\" WAVE\n" +
            "  TRACK 01 AUDIO\n" +
            "    TITLE \"Tone\"\n" +
            "    INDEX 01 00:00:00\n");
        return dir;
    }

    [AvaloniaFact]
    public void VerifyViewBindsDiscoveryResultAndRendersWorkspace()
    {
        string dir = WriteFixtureAlbum();
        try
        {
            var viewModel = new VerifyViewModel(
                new FakeVerifyService(),
                new ReportStore(),
                new VerificationSourceDiscovery());

            var view = new VerifyView { DataContext = viewModel };
            var window = new Window { Content = view };
            window.Show();

            Assert.True(viewModel.LoadSources(new[] { dir }));
            Assert.True(viewModel.HasSource);
            Assert.Single(viewModel.Discs);
            // Discovery names the disc by the cue file's stem, matching the
            // WPF app's behavior for the same input.
            Assert.Equal("album", viewModel.Discs[0].DisplayName);

            window.Close();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
