using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using CUETools.Linux.App.Services;
using Xunit;

namespace CUETools.Linux.Tests;

// The filament fade between themes. The theme change itself must never depend
// on the animation: the apply action runs even when capture fails, and the
// overlay always ends hidden and released.
public class ThemeCrossfadeTests
{
    [AvaloniaFact]
    public async Task TheOverlayCarriesTheOldFrameAndEndsHiddenAndReleased()
    {
        var content = new Border { Background = Brushes.DarkSlateGray };
        var overlay = new Image { IsVisible = false, IsHitTestVisible = false };
        var window = new Window
        {
            Width = 300, Height = 200,
            Content = new Panel { Children = { content, overlay } },
        };
        window.Show();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        bool applied = false;
        Task run = ThemeCrossfade.Run(content, overlay, goingDark: true, () => applied = true);

        Assert.True(applied);                    // the theme flip precedes the fade
        Assert.True(overlay.IsVisible);
        Assert.NotNull(overlay.Source);          // the old theme's frame is held

        Task done = await Task.WhenAny(run, Task.Delay(5000));
        Assert.Same(run, done);                  // the fade completes on the real clock
        Assert.False(overlay.IsVisible);
        Assert.Null(overlay.Source);             // frame released
        window.Close();
    }

    [AvaloniaFact]
    public async Task AFailedCaptureStillAppliesTheThemeAndSkipsTheFade()
    {
        // zero-size root: capture returns null, the change must still land
        var content = new Border { Width = 0, Height = 0 };
        var overlay = new Image { IsVisible = false };
        var window = new Window { Content = new Panel { Children = { content, overlay } } };
        window.Show();

        bool applied = false;
        await ThemeCrossfade.Run(content, overlay, goingDark: false, () => applied = true);

        Assert.True(applied);
        Assert.False(overlay.IsVisible);
        window.Close();
    }
}
