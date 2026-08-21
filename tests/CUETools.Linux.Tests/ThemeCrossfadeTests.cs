using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
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
        // the content deliberately leaves the corner bare (margin, no panel
        // background): the held frame must still be opaque there, from the
        // window's own background - the transparent-corner capture was the
        // white flash (the compositor clears white behind the holes)
        var content = new Border { Background = Brushes.DarkSlateGray, Margin = new Thickness(20) };
        var overlay = new Image { IsVisible = false, IsHitTestVisible = false };
        var window = new Window
        {
            Width = 300, Height = 200,
            Background = Brushes.DarkOliveGreen,
            Content = new Panel { Children = { content, overlay } },
        };
        window.Show();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        bool applied = false;
        Task run = ThemeCrossfade.Run(content, overlay, goingDark: true, () => applied = true);

        Assert.True(applied);                    // the theme flip precedes the fade
        Assert.True(overlay.IsVisible);
        Assert.NotNull(overlay.Source);          // the old theme's frame is held
        // and that frame must be OPAQUE at every pixel the content leaves bare,
        // or the compositor's white clear color flashes through mid-swap
        AssertCornerOpaque((RenderTargetBitmap)overlay.Source!);

        Task done = await Task.WhenAny(run, Task.Delay(5000));
        Assert.Same(run, done);
        Assert.False(overlay.IsVisible);
        Assert.Null(overlay.Source);             // frame released
        // The flash pin: the overlay must REST at zero. The old keyframe
        // animation reverted opacity to its base value of 1 on completion,
        // which flashed the old theme at full strength for a frame; a
        // transition's destination is the base value, so this holds even
        // when no clock ever ticks.
        Assert.Equal(0, overlay.Opacity);
        window.Close();
    }

    private static void AssertCornerOpaque(RenderTargetBitmap frame)
    {
        // The default committed harness uses headless FAKE drawing, whose bitmaps
        // carry garbage pixels; the pixel pin is meaningful only under the Skia
        // render-verify harness (where it caught the transparent-corner capture,
        // corner A=0). Calibrate: if the renderer cannot reproduce a solid color,
        // skip the pixel check and let the lifecycle assertions stand.
        if (!RendererPaintsRealPixels())
            return;
        Assert.Equal(255, PixelAlphaAt(frame, 0, 0));
    }

    private static bool _calibrated;
    private static bool _realPixels;

    private static bool RendererPaintsRealPixels()
    {
        if (_calibrated)
            return _realPixels;
        var probe = new RenderTargetBitmap(new PixelSize(4, 4), new Vector(96, 96));
        probe.Render(new Border { Width = 4, Height = 4, Background = Brushes.Red });
        _realPixels = PixelAlphaAt(probe, 0, 0) == 255;
        _calibrated = true;
        return _realPixels;
    }

    private static byte PixelAlphaAt(RenderTargetBitmap frame, int x, int y)
    {
        using var ms = new MemoryStream();
        frame.Save(ms, new PngBitmapEncoderOptions());
        ms.Position = 0;
        using var decoded = new Bitmap(ms);
        int w = decoded.PixelSize.Width;
        nint buf = Marshal.AllocHGlobal(w * 4);
        try
        {
            decoded.CopyPixels(new PixelRect(0, y, w, 1), buf, w * 4, w * 4);
            return Marshal.ReadByte(buf, x * 4 + 3);   // BGRA
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
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
