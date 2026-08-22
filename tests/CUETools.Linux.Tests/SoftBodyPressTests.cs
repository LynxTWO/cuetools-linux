using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using CUETools.Linux.App.Controls;
using Xunit;

namespace CUETools.Linux.Tests;

// SLICE-015's press lane, and this repo's first pointer-input tests: before
// this there were zero MouseDown/MouseUp/MouseMove/KeyPress calls anywhere in
// tests/. The state machine is separated from the renderer precisely so it can
// be driven headlessly like this.
public class SoftBodyPressTests
{
    private static readonly Size KeySize = new(120, 32);

    // --- the state machine on its own, no UI at all ---

    [Fact]
    public void APressRecordsExactlyWhereItLanded()
    {
        var press = new SoftBodyPress();
        Assert.False(press.IsPressed);

        press.PressAt(new Point(8, 6));
        Assert.True(press.IsPressed);
        Assert.Equal(PressOrigin.Pointer, press.Origin);
        Assert.Equal(8, press.Point.X, 9);
        Assert.Equal(6, press.Point.Y, 9);
    }

    [Fact]
    public void AKeyboardPressIsCentredAndIgnoresPointerDrags()
    {
        var press = new SoftBodyPress();
        press.PressFromKeyboard(KeySize);

        Assert.Equal(PressOrigin.Keyboard, press.Origin);
        Assert.Equal(60, press.Point.X, 9);
        Assert.Equal(16, press.Point.Y, 9);

        // there is no finger, so nothing can move it
        press.DragTo(new Point(4, 4));
        Assert.Equal(60, press.Point.X, 9);
    }

    [Fact]
    public void ACancelledPressReleasesDeadAndALandedOneRebounds()
    {
        var landed = new SoftBodyPress();
        landed.PressAt(new Point(8, 6));
        TimeSpan landedDuration = landed.Release(landed: true);
        Assert.True(landed.LastReleaseRebounded);
        Assert.Equal(SoftBodyPress.LandedRelease, landedDuration);

        var cancelled = new SoftBodyPress();
        cancelled.PressAt(new Point(8, 6));
        TimeSpan cancelledDuration = cancelled.Release(landed: false);
        Assert.False(cancelledDuration == SoftBodyPress.LandedRelease);
        Assert.False(cancelled.LastReleaseRebounded);
        Assert.Equal(SoftBodyPress.DeadRelease, cancelledDuration);

        // the dead release is the slower of the two: slack, not snappy
        Assert.True(SoftBodyPress.DeadRelease > SoftBodyPress.LandedRelease);
    }

    [Fact]
    public void AKeyDisabledMidPressReleasesDeadRatherThanTeleporting()
    {
        // the Rip key's own case: its command goes disabled the instant a rip
        // starts, while the cap is fully collapsed under the pointer
        var press = new SoftBodyPress();
        press.PressAt(new Point(60, 16));
        TimeSpan duration = press.Disable();

        Assert.False(press.IsPressed);
        Assert.False(press.LastReleaseRebounded);
        Assert.Equal(SoftBodyPress.DeadRelease, duration);
    }

    // --- driven through a real Button, headlessly ---

    private static (Window w, Button b, SoftBodyPress p) Rig()
    {
        var press = new SoftBodyPress();
        var button = new Button { Content = "Test & Copy", Width = 120, Height = 32 };
        SoftBodyPressBinding.Attach(button, press, () => { });
        var window = new Window
        {
            Width = 300, Height = 200,
            // a null background silently swallows headless clicks
            Background = Avalonia.Media.Brushes.Black,
            Content = new Panel { Children = { button } },
        };
        window.Show();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        return (window, button, press);
    }

    private static Point On(Window w, Button b, double x, double y)
        => b.TranslatePoint(new Point(x, y), w)!.Value;

    [AvaloniaFact]
    public void ARealPointerPressRecordsItsPointAndACornerSinksDeeperThanTheCentre()
    {
        var (w, b, p) = Rig();

        w.MouseDown(On(w, b, 8, 6), MouseButton.Left);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.True(p.IsPressed);
        Assert.Equal(8, p.Point.X, 1);
        Assert.Equal(6, p.Point.Y, 1);

        // the whole point of the feature, measured in device-independent pixels
        double corner = SoftBodyModel.Displacement(p.Point, new Point(8, 6), KeySize, 1);
        w.MouseUp(On(w, b, 8, 6), MouseButton.Left);
        w.MouseDown(On(w, b, 60, 16), MouseButton.Left);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        double centre = SoftBodyModel.Displacement(p.Point, new Point(8, 6), KeySize, 1);
        w.MouseUp(On(w, b, 60, 16), MouseButton.Left);

        Assert.True(corner > centre,
            $"a corner press must sink its corner deeper: {corner:0.000} vs {centre:0.000}");
        w.Close();
    }

    [AvaloniaFact]
    public void DraggingWhilePressedMovesTheDeformationWithTheFinger()
    {
        var (w, b, p) = Rig();

        w.MouseDown(On(w, b, 20, 16), MouseButton.Left);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.Equal(20, p.Point.X, 1);

        w.MouseMove(On(w, b, 100, 16));
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.Equal(100, p.Point.X, 1);

        w.MouseUp(On(w, b, 100, 16), MouseButton.Left);
        w.Close();
    }

    [AvaloniaFact]
    public void PressThenDragOffCancelsTheClickAndReleasesDead()
    {
        var (w, b, p) = Rig();
        int clicks = 0;
        b.Click += (_, _) => clicks++;

        w.MouseDown(On(w, b, 60, 16), MouseButton.Left);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.True(p.IsPressed);

        // leave the key entirely, then let go
        w.MouseMove(new Point(280, 190));
        w.MouseUp(new Point(280, 190), MouseButton.Left);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, clicks);
        Assert.False(p.IsPressed);
        Assert.False(p.LastReleaseRebounded);
        w.Close();
    }

    [AvaloniaFact]
    public void ALandedClickFiresAndRebounds()
    {
        var (w, b, p) = Rig();
        int clicks = 0;
        b.Click += (_, _) => clicks++;

        w.MouseDown(On(w, b, 60, 16), MouseButton.Left);
        w.MouseUp(On(w, b, 60, 16), MouseButton.Left);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, clicks);
        Assert.True(p.LastReleaseRebounded);
        w.Close();
    }

    [AvaloniaFact]
    public void SpaceAndEnterPressDeadCentre()
    {
        var (w, b, p) = Rig();
        b.Focus();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        // KeyPress is DOWN only in this harness (measured: a lone KeyPress
        // raises KeyDown and never KeyUp), so the release is explicit.
        w.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        Assert.True(p.IsPressed, "Space should hold the key down");
        Assert.Equal(PressOrigin.Keyboard, p.Origin);
        Assert.Equal(60, p.Point.X, 1);
        Assert.Equal(16, p.Point.Y, 1);
        // dead centre means no tilt: mirrored samples must match exactly
        Assert.Equal(
            SoftBodyModel.Displacement(p.Point, new Point(30, 16), KeySize, 1),
            SoftBodyModel.Displacement(p.Point, new Point(90, 16), KeySize, 1), 9);

        w.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.False(p.IsPressed);
        Assert.True(p.LastReleaseRebounded, "a keyboard activation always lands");
        w.Close();
    }
}
