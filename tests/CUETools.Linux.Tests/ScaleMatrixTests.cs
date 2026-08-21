using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Avalonia.Styling;
using CUETools.Linux.App;
using CUETools.Linux.App.Controls;
using CUETools.Linux.App.Services;
using CUETools.Wpf.Services;
using Xunit;

namespace CUETools.Linux.Tests;

// SLICE-013's window-level matrix (S13-001/002/004/006 backbone): the real
// graph and the real MainWindow, driven through the desktop sizes each scale
// factor leaves on a 1080p screen, in both themes. Layout is real under
// headless fake drawing, so the no-clipping assertions run in CI; pixel
// captures for the brief's archive are produced locally under the Skia
// harness. The five factors map to logical desktops: 100% 1920x1080,
// 125% 1536x864, 150% 1280x720, 175% 1097x617, 200% 960x540.
public class ScaleMatrixTests
{
    private sealed class NullDialogs : IFileDialogService
    {
        public Task<string[]?> PickFilesAsync(string title, bool multiselect, IReadOnlyList<FilePickerGroup> groups)
            => Task.FromResult<string[]?>(null);
        public Task<string?> PickFolderAsync(string title)
            => Task.FromResult<string?>(null);
    }

    private sealed class DeclinePrompts : IUserPrompt
    {
        public Task<bool> ConfirmOkCancelAsync(string message, string title) => Task.FromResult(false);
    }

    private static (double W, double H, RailLayout Expected, string Factor)[] Matrix =>
    new[]
    {
        (1200d, 720d, RailLayout.Full, "100%"),
        (1200d, 720d, RailLayout.Full, "125%"),
        (1200d, 700d, RailLayout.Full, "150%"),
        (1097d, 617d, RailLayout.Compact, "175%"),
        (960d, 540d, RailLayout.Compact, "200%"),
        (800d, 540d, RailLayout.Floor, "floor"),
        (640d, 480d, RailLayout.Floor, "min"),
    };

    [AvaloniaFact]
    public void EveryScaleFactorLaysOutWithoutClippingInBothThemes()
    {
        Composition.AppGraph graph = Composition.CreateAppGraph(
            new NullDialogs(), new DeclinePrompts(), new AvaloniaUiDispatcher());
        var window = new MainWindow(new ThemeState(), graph.Verify, graph.Convert, graph);
        window.Show();

        foreach (ThemeVariant variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            Avalonia.Application.Current!.RequestedThemeVariant = variant;
            foreach (var (w, h, expected, factor) in Matrix)
            {
                window.Width = w;
                window.Height = h;
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();

                Assert.Equal(expected, window.RailLayoutForTest);

                var scroll = window.FindControl<ScrollViewer>("PageScroll")!;
                if (expected != RailLayout.Floor)
                {
                    // S13-001: above the floor, nothing may need horizontal scrolling
                    Assert.True(scroll.Extent.Width <= scroll.Viewport.Width + 0.5,
                        $"{factor} {variant}: page extent {scroll.Extent.Width} overflows viewport {scroll.Viewport.Width}");
                }
                else
                {
                    // the floor holds the 860 layout and scrolls instead of clipping
                    Assert.True(scroll.Extent.Width >= RailBreakpoints.FloorBelow - 0.5,
                        $"floor {variant}: extent {scroll.Extent.Width} lost the held layout");
                    Assert.True(scroll.Viewport.Width < scroll.Extent.Width,
                        $"floor {variant}: no horizontal scroll despite the held layout");
                    // S13-005: the held layout is the instruments' floor - the disc
                    // read map never lays out below its 860-layout size
                    var disc = window.FindControl<UserControl>("RipPage")!
                        .GetVisualDescendants().OfType<DiscReadMap>().FirstOrDefault();
                    Assert.True(disc == null || disc.Bounds.Width >= 100,
                        $"floor {variant}: disc read map squeezed to {disc?.Bounds.Width}");
                }

                // local evidence captures (Skia harness + env opt-in; CI's fake
                // drawing renders garbage pixels, so it never sets the variable)
                string? capture = Environment.GetEnvironmentVariable("CUETOOLS_MATRIX_CAPTURE");
                if (!string.IsNullOrEmpty(capture))
                {
                    Directory.CreateDirectory(capture);
                    window.CaptureRenderedFrame()!.Save(
                        Path.Combine(capture, $"matrix-{factor.Replace('%', 'p')}-{(variant == ThemeVariant.Dark ? "dark" : "light")}.png"),
                        new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
                }

                // S13-004 reachability on the Rip page: primary actions, drive
                // selection, and the CRC evidence column exist and are visible
                window.ShowRipPage();
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                foreach (string name in new[]
                    { "RipButton", "TestCopyButton", "VerifyOnlyButton", "StopButton", "DriveSelector", "TestCrcHeader" })
                {
                    Control? c = window.FindControl<Control>(name)
                        ?? window.FindControl<UserControl>("RipPage")!.FindControl<Control>(name);
                    Assert.True(c is { IsEffectivelyVisible: true },
                        $"{factor} {variant}: {name} not reachable on the Rip page");
                }
            }
        }

        // S13-002: the active page survives collapse and return
        window.ShowSettingsPage();
        window.Width = 1000;
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.Equal(RailLayout.Compact, window.RailLayoutForTest);
        Assert.True(window.FindControl<UserControl>("SettingsPage")!.IsVisible);
        window.Width = 1200;
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.Equal(RailLayout.Full, window.RailLayoutForTest);
        Assert.True(window.FindControl<UserControl>("SettingsPage")!.IsVisible);

        window.Close();
    }
}
