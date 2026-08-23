using Avalonia;

namespace CUETools.Linux.App.Controls;

/// <summary>
/// The deformation field of a rubber key over a center plunger (SLICE-015).
/// Pure arithmetic: no UI, no rendering, no clock, so every rule below is
/// testable without a renderer, which is the only lane that runs in CI.
///
/// The physical story, which the four terms mirror one for one. A soft rubber
/// cap is bonded to the console around its skirt and sits on a dome switch at
/// its CENTER. Press it anywhere and three things happen at once: the plunger
/// collapses (the cap travels down), the cap rocks about that plunger like a
/// lever (so a corner press drives its own corner down and lifts the far one),
/// and the rubber dimples locally under the finger. The bond line does not
/// move at all, which is the fourth term and the reason nothing ever escapes
/// the control's own rectangle.
/// </summary>
public static class SoftBodyModel
{
    /// <summary>Maximum displacement at the deepest point of a full press, in
    /// device-independent pixels. The whole field is normalised against this,
    /// so no combination of press point and size can exceed it. An early
    /// version summed un-normalised gains and punched 2.8 budgets through the
    /// console floor.</summary>
    public const double DepthBudget = 6.0;

    /// <summary>How much of the budget the plunger's own collapse claims when
    /// pressed dead center.</summary>
    public const double TravelShare = 0.46;

    /// <summary>How much the lever action claims at the press point when
    /// pressed at a corner.</summary>
    public const double TiltShare = 0.32;

    /// <summary>How much the local rubber dimple claims under the finger.</summary>
    public const double DimpleShare = 0.22;

    /// <summary>Radius of the dimple in DIP, as a Gaussian sigma. Absolute, not
    /// relative to the key: the rubber's stiffness does not change because the
    /// key is wider. Normalising this per axis smeared the dimple across the
    /// whole width of a 3:1 key.</summary>
    public const double DimpleSigma = 4.5;

    /// <summary>Width of the bonded skirt in DIP. Displacement falls to exactly
    /// zero across this band, so the perimeter never moves.</summary>
    public const double SkirtWidth = 3.0;

    /// <summary>How far off center a press has to be before the plunger stops
    /// taking the full load, in fractions of the half-diagonal. Tight on
    /// purpose: a dome switch needs force near its own axis, so a corner press
    /// mostly ROCKS the cap and only partly collapses the dome. At 0.85 the
    /// plunger still took 60 percent of a corner press's load, which buried
    /// the lever and left the far corner sinking instead of rising - the cap
    /// read as a slab on a spring rather than a lever on a pivot.</summary>
    private const double PlungerReach = 0.55;

    /// <summary>How far past the key's edge a held press keeps handing its load
    /// over, in DIP. Slide a finger off a key you are holding and the pressure
    /// transfers off the cap over roughly a fingertip's width, not instantly
    /// and not never. Absolute for the same reason the dimple is: how rubber
    /// lets go does not change because the key is wider.</summary>
    public const double DisengageBand = 14.0;

    /// <summary>The point on the cap the finger actually bears on. A finger
    /// cannot load a point that is not on the key, so once the pointer leaves,
    /// contact stays at the nearest point on the surface: it is sliding along
    /// the edge, not pressing thin air an inch away.
    ///
    /// Leaving this out was a real defect (owner report 2026-08-23). The tilt
    /// term divides the press offset by the key's own half-diagonal, so an
    /// unclamped press point off the key drove that ratio past 1 without
    /// limit - 500 DIP off a 40x30 key reached 20 - and tilt scales on it
    /// directly. Deformation hit 2.5x the depth budget, and worse the smaller
    /// the key, because the same drag divides by a smaller half-diagonal AND
    /// projects onto a shorter reach. The budget is only a ceiling while
    /// contact is on the cap.</summary>
    public static Point Contact(Point press, Size size) => new(
        Math.Clamp(press.X, 0, Math.Max(0, size.Width)),
        Math.Clamp(press.Y, 0, Math.Max(0, size.Height)));

    /// <summary>How much of the press still reaches the cap: 1 while the
    /// pointer is on the key, easing to 0 across <see cref="DisengageBand"/>
    /// once it is not. Smooth at both ends, so grazing the edge does not make
    /// the key flick.
    ///
    /// This also makes the motion honest. Releasing out here does not activate
    /// the button, and a key that stayed fully depressed the whole time would
    /// be promising an outcome that is not coming.</summary>
    public static double Engagement(Point press, Size size)
    {
        if (size.Width <= 0 || size.Height <= 0)
            return 0;
        double outside = Distance(press, Contact(press, size));
        if (outside <= 0)
            return 1;
        if (outside >= DisengageBand)
            return 0;
        double t = 1 - outside / DisengageBand;
        return t * t * (3 - 2 * t);
    }

    /// <summary>
    /// Displacement at one point of the cap, in DIP, positive INTO the key.
    /// A negative result is a genuine lift: the far side of a lever rises.
    /// </summary>
    /// <param name="press">Where the pointer is, in DIP from the key's top
    /// left. May be outside the key: a press can be dragged anywhere while the
    /// button holds capture, and the contact and disengage terms above handle
    /// it.</param>
    /// <param name="sample">The point being displaced.</param>
    /// <param name="size">The key's size in DIP.</param>
    /// <param name="amount">Press amount, 0 at rest, 1 at full press. Values
    /// above 1 are allowed so a test can exaggerate the field.</param>
    public static double Displacement(Point press, Point sample, Size size, double amount)
    {
        if (amount <= 0 || size.Width <= 0 || size.Height <= 0)
            return 0;

        // the load that still reaches the cap, and where it bears
        amount *= Engagement(press, size);
        if (amount <= 0)
            return 0;
        press = Contact(press, size);

        var center = new Point(size.Width / 2, size.Height / 2);
        double halfDiag = Distance(center, new Point(0, 0));
        if (halfDiag <= 0)
            return 0;

        // (1) plunger travel: the whole cap sinks, less so the further from the
        // switch the load is applied, because a lever wastes part of it.
        double offCenter = Distance(press, center) / halfDiag;
        double plungerLoad = Gaussian(offCenter, PlungerReach);
        double travel = TravelShare * plungerLoad;

        // (2) tilt about the plunger: a rigid rock, so the far side comes UP by
        // as much as the near side goes down. The lever arm is how far off
        // center the press is; the response is how far off center the sample is,
        // projected onto the press direction.
        double tilt = 0;
        if (offCenter > 1e-9)
        {
            double dirX = (press.X - center.X) / (offCenter * halfDiag);
            double dirY = (press.Y - center.Y) / (offCenter * halfDiag);
            double reach = ((sample.X - center.X) * dirX + (sample.Y - center.Y) * dirY) / halfDiag;
            tilt = TiltShare * offCenter * reach;
        }

        // (3) the local dimple, in absolute DIP so a wide key does not smear it.
        double dimple = DimpleShare * Gaussian(Distance(sample, press), DimpleSigma);

        // (4) the bonded skirt: everything above is scaled to zero at the rim.
        double rim = Rim(sample, size);

        return rim * (travel + tilt + dimple) * amount * DepthBudget;
    }

    /// <summary>How free the cap is to move at this point: 1 in the interior,
    /// falling smoothly to exactly 0 at the bond line.</summary>
    public static double Rim(Point sample, Size size)
    {
        double edgeX = Math.Min(sample.X, size.Width - sample.X);
        double edgeY = Math.Min(sample.Y, size.Height - sample.Y);
        // a key thinner than two skirts is all skirt; clamp so it cannot invert
        double skirtX = Math.Min(SkirtWidth, size.Width / 2);
        double skirtY = Math.Min(SkirtWidth, size.Height / 2);
        return SmoothStep(edgeX, skirtX) * SmoothStep(edgeY, skirtY);
    }

    /// <summary>0 at or outside the edge, 1 at or beyond the skirt, smooth between.</summary>
    private static double SmoothStep(double distanceFromEdge, double skirt)
    {
        if (skirt <= 0) return distanceFromEdge > 0 ? 1 : 0;
        double t = Math.Clamp(distanceFromEdge / skirt, 0, 1);
        return t * t * (3 - 2 * t);
    }

    private static double Gaussian(double distance, double sigma)
        => Math.Exp(-(distance * distance) / (2 * sigma * sigma));

    private static double Distance(Point a, Point b)
    {
        double dx = a.X - b.X, dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>The press point a keyboard activation uses: dead center, so
    /// Space and Enter press straight down with no tilt (there is no pointer to
    /// take a position from, and inventing one would be a lie about where the
    /// user pressed).</summary>
    public static Point KeyboardPress(Size size) => new(size.Width / 2, size.Height / 2);
}
