using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace CUETools.Linux.App.Controls;

/// <summary>Where a press came from, which decides how it behaves.</summary>
public enum PressOrigin
{
    None,

    /// <summary>A pointer, which has a real position on the key.</summary>
    Pointer,

    /// <summary>Space, Enter, or an automation peer. No position exists, so the
    /// press is dead center (SoftBodyModel.KeyboardPress) rather than inventing
    /// a point the user never touched.</summary>
    Keyboard,
}

/// <summary>
/// The press state of one soft-body key (SLICE-015): where it was pressed, how
/// hard, and - the part that carries meaning - whether the press LANDED.
///
/// A press that is dragged off the key before release does not raise Click, and
/// today that mismatch is invisible because the depression is 1.2 px. Once the
/// rubber becomes the app's press affordance it becomes the thing users trust,
/// so a cancelled press releases DEAD (no rebound) while a landed one rebounds
/// with the rubbery overshoot. Same house rule as everywhere else here: do not
/// let a refused thing look like a success.
/// </summary>
public sealed class SoftBodyPress
{
    /// <summary>Rebound of a landed press: a soft overshoot, like rubber.</summary>
    public static readonly TimeSpan LandedRelease = TimeSpan.FromMilliseconds(190);

    /// <summary>Release of a cancelled press, and of a key that goes disabled
    /// mid-press: critically damped, no overshoot, slightly slower. It should
    /// feel like the key going slack, not like it worked.</summary>
    public static readonly TimeSpan DeadRelease = TimeSpan.FromMilliseconds(240);

    public PressOrigin Origin { get; private set; }
    public Point Point { get; private set; }
    public bool IsPressed => Origin != PressOrigin.None;

    /// <summary>True when the last release rebounded because the click landed.
    /// False after a cancelled press or a press-to-disabled transition.</summary>
    public bool LastReleaseRebounded { get; private set; }

    public void PressAt(Point point)
    {
        Origin = PressOrigin.Pointer;
        Point = point;
    }

    public void PressFromKeyboard(Size size)
    {
        Origin = PressOrigin.Keyboard;
        Point = SoftBodyModel.KeyboardPress(size);
    }

    /// <summary>The pointer moved while held down. A pointer press follows the
    /// finger; a keyboard press ignores it, because there is no finger.</summary>
    public void DragTo(Point point)
    {
        if (Origin == PressOrigin.Pointer)
            Point = point;
    }

    /// <summary>Release. <paramref name="landed"/> is whether the click actually
    /// fired, which is what decides the rebound.</summary>
    public TimeSpan Release(bool landed)
    {
        Origin = PressOrigin.None;
        LastReleaseRebounded = landed;
        return landed ? LandedRelease : DeadRelease;
    }

    /// <summary>The key went disabled while held. The cap must not teleport from
    /// full travel to flat: it runs the dead release and freezes unpowered. The
    /// Rip key does exactly this - its command's CanExecute goes false the
    /// instant a rip starts, while the cap is fully collapsed under the pointer.</summary>
    public TimeSpan Disable() => Release(landed: false);
}

/// <summary>Wiring a real Button's input into a <see cref="SoftBodyPress"/>.
/// Separated from the renderer so the state machine can be tested headlessly
/// without anything drawing.</summary>
public static class SoftBodyPressBinding
{
    private static bool Inside(Button button, Point local)
        => local.X >= 0 && local.Y >= 0
           && local.X <= button.Bounds.Width && local.Y <= button.Bounds.Height;

    public static void Attach(Button button, SoftBodyPress press, Action invalidate)
    {
        button.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
        {
            press.PressAt(e.GetPosition(button));
            invalidate();
        }, RoutingStrategies.Tunnel);

        button.AddHandler(InputElement.PointerMovedEvent, (_, e) =>
        {
            if (!press.IsPressed) return;
            press.DragTo(e.GetPosition(button));
            invalidate();
        }, RoutingStrategies.Tunnel);

        button.AddHandler(InputElement.PointerReleasedEvent, (_, e) =>
        {
            if (!press.IsPressed) return;
            // Landed is decided by geometry, deliberately. Measured event order
            // on a real Button: PointerReleased fires BEFORE IsPressed clears and
            // before Click, so reading button.IsPressed here reports true for a
            // dragged-off release too and every cancel would be rewarded. The
            // pointer being inside the bounds is the same rule Avalonia itself
            // uses to decide whether Click fires.
            press.Release(landed: Inside(button, e.GetPosition(button)));
            invalidate();
        }, RoutingStrategies.Tunnel);

        // handledEventsToo, because Button handles Space and Enter itself and
        // marks them handled before a plain CLR subscription would ever see them
        button.AddHandler(InputElement.KeyDownEvent, (_, e) =>
        {
            if (e.Key is not (Key.Space or Key.Enter) || press.IsPressed) return;
            press.PressFromKeyboard(button.Bounds.Size);
            invalidate();
        }, RoutingStrategies.Bubble, handledEventsToo: true);

        button.AddHandler(InputElement.KeyUpEvent, (_, e) =>
        {
            if (e.Key is not (Key.Space or Key.Enter) || !press.IsPressed) return;
            // a keyboard activation cannot be dragged off, so it always lands
            press.Release(landed: true);
            invalidate();
        }, RoutingStrategies.Bubble, handledEventsToo: true);

        button.PropertyChanged += (_, e) =>
        {
            if (e.Property != InputElement.IsEnabledProperty) return;
            if (!button.IsEnabled && press.IsPressed)
            {
                press.Disable();
                invalidate();
            }
        };
    }
}
