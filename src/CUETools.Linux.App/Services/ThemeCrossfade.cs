using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Styling;

namespace CUETools.Linux.App.Services;

/// <summary>
/// The filament fade between themes: the old theme's last frame is held in an
/// overlay image while the new theme lays out under it, then the frame dims
/// away. Going dark takes the long tail of a cooling filament; coming up to
/// light is quicker, the way a bulb lights faster than it cools. A failed
/// capture never blocks the change; the theme just snaps like before.
/// </summary>
public static class ThemeCrossfade
{
    public static readonly TimeSpan ToDark = TimeSpan.FromMilliseconds(560);
    public static readonly TimeSpan ToLight = TimeSpan.FromMilliseconds(300);

    private static int _generation;

    /// <summary>Capture <paramref name="root"/> into <paramref name="overlay"/>, apply the
    /// theme, then fade the captured frame out. The overlay must sit above the themed
    /// content and never hit-test. A second toggle mid-fade recaptures the blend, so
    /// rapid flips stay continuous instead of flashing.</summary>
    public static async Task Run(Control root, Image overlay, bool goingDark, Action applyTheme)
    {
        RenderTargetBitmap? frame = Capture(root);
        applyTheme();
        if (frame == null)
            return;

        int gen = ++_generation;
        overlay.Source = frame;
        overlay.Opacity = 1;
        overlay.IsVisible = true;
        var fade = new Animation
        {
            Duration = goingDark ? ToDark : ToLight,
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(Visual.OpacityProperty, 1.0) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(Visual.OpacityProperty, 0.0) } },
            },
        };
        await fade.RunAsync(overlay);
        if (gen == _generation)
        {
            overlay.IsVisible = false;
            overlay.Source = null;
        }
        frame.Dispose();
    }

    private static RenderTargetBitmap? Capture(Control root)
    {
        try
        {
            // Render the whole top level, not the content control: the window paints
            // its background, so every pixel of the held frame is opaque. A content
            // grid leaves its gaps transparent, and the compositor's white clear
            // color flashed through those holes mid-swap (owner-reported, pixel-
            // probed: grid capture corner A=0, window capture A=255).
            var top = TopLevel.GetTopLevel(root);
            Visual target = top ?? root;
            Rect bounds = top?.Bounds ?? root.Bounds;
            double scale = top?.RenderScaling ?? 1.0;
            var size = new PixelSize(
                (int)Math.Ceiling(bounds.Width * scale),
                (int)Math.Ceiling(bounds.Height * scale));
            if (size.Width <= 0 || size.Height <= 0)
                return null;
            var bmp = new RenderTargetBitmap(size, new Vector(96 * scale, 96 * scale));
            bmp.Render(target);
            return bmp;
        }
        catch
        {
            return null;
        }
    }
}
