using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace CUETools.Linux.App.Controls;

/// <summary>
/// SLICE-015 step 3, the owner gate: the cheap path. A projective transform on
/// the key face plus pointer-following shading, no Skia and no mesh. It
/// delivers everything the expensive renderer would EXCEPT a moving outline,
/// and D-080 pinned the rim, so the question this build exists to answer is
/// whether the shaded dip reads as "below the surface" without one.
///
/// Enabled by CUETOOLS_SOFTBODY=1 so it can be A/B'd against today's keys in
/// the same session.
/// </summary>
public static class SoftBodyKey
{
    public static bool Enabled { get; } =
        Environment.GetEnvironmentVariable("CUETOOLS_SOFTBODY") == "1";

    /// <summary>How far a displaced point pulls toward the vanishing point, per
    /// DIP of depth. This is what turns depth into something the eye reads as
    /// depth on a flat screen: a corner pushed in recedes.</summary>
    private const double Perspective = 0.024;

    /// <summary>How far a displaced point slides down-screen per DIP of depth.
    /// The bench is lit from above, so sinking also means sliding into shadow.</summary>
    private const double Drop = 0.80;

    /// <summary>Housing-lamp levels, matched to the style setters on keySeam so
    /// the ramp starts exactly where hover left off and hands back to the same
    /// value. If these drift apart the light jumps at the start of a press.</summary>
    public const double SeamHover = 0.34;
    public const double SeamPressed = 0.85;

    public static void Attach(Button button)
    {
        if (!Enabled) return;
        var press = new SoftBodyPress();
        double amount = 0;
        DispatcherTimerLoop? settle = null;

        void Apply()
        {
            if (button.GetVisualDescendant("keyFace") is not Border face) return;
            // D-084 trap 1: a TransformOperationsTransition flattens a
            // perspective matrix to its affine part, silently. Clear it on the
            // element we are about to drive.
            face.Transitions = null;
            var size = new Size(button.Bounds.Width, button.Bounds.Height);
            if (size.Width <= 0 || size.Height <= 0) return;

            if (button.GetVisualDescendant("keyRecess") is Border recess)
                recess.Opacity = amount <= 0.001 ? 0 : 1;

            // The housing lamp comes up with the press, the way a console key
            // brightens as it goes down, rather than switching between two
            // style states. Handing it back matters: a locally set value
            // outranks a style setter, so leaving one here would kill the
            // hover glow permanently the first time the key was pressed. Same
            // trap as D-084 (2), one property along.
            if (button.GetVisualDescendant("keySeam") is Border seam)
            {
                if (amount <= 0.001)
                    seam.ClearValue(Visual.OpacityProperty);
                else
                    seam.Opacity = SeamHover + amount * (SeamPressed - SeamHover);
            }

            if (amount <= 0.001)
            {
                face.RenderTransform = null;
                SetDip(button, null, 0);
                return;
            }

            face.RenderTransform = new MatrixTransform(
                Homography(press.Point, size, amount));
            SetDip(button, press.Point, amount);
        }

        SoftBodyPressBinding.Attach(button, press, () =>
        {
            settle?.Stop();
            if (press.IsPressed)
            {
                // collapse fast; rubber gives way quickly under a finger
                settle = DispatcherTimerLoop.Ramp(amount, 1.0, TimeSpan.FromMilliseconds(90),
                    v => { amount = v; Apply(); });
            }
            else
            {
                // D-080/S15: a landed click rebounds with a rubbery overshoot;
                // a cancelled press, or one whose key went disabled, goes slack
                bool landed = press.LastReleaseRebounded;
                settle = DispatcherTimerLoop.Ramp(amount, 0,
                    landed ? SoftBodyPress.LandedRelease : SoftBodyPress.DeadRelease,
                    v => { amount = v; Apply(); }, overshoot: landed);
            }
        });
    }

    /// <summary>The dip's shading: a soft dark well centred on the press point,
    /// with a light rim just outside it where the rubber bulges. This is what
    /// has to carry "below the surface" when the outline cannot move.</summary>
    private static void SetDip(Button button, Point? press, double amount)
    {
        if (button.GetVisualDescendant("keyDip") is not Border dip) return;
        if (press is not { } p || amount <= 0.001)
        {
            dip.Opacity = 0;
            return;
        }
        double w = Math.Max(1, button.Bounds.Width), h = Math.Max(1, button.Bounds.Height);
        bool dark = button.ActualThemeVariant != Avalonia.Styling.ThemeVariant.Light;
        // A near-black cap has almost no room left to darken, so on the dark
        // bench the well reads by LOSING its specular and gaining a faint lift
        // around the rim, the way a dent in a glossy surface does. On the light
        // bench there is plenty of range and a plain soft shadow is the honest
        // and stronger cue.
        // With the cap carrying a real crown highlight, the well's job is to
        // SUPPRESS that specular locally, which is what a dent in glossy rubber
        // actually does. An earlier version added its own bright rim and drew
        // crescent-shaped light artifacts across the key instead.
        var stops = dark
            ? new GradientStops
            {
                new GradientStop(Color.FromArgb(0x86, 0, 0, 0), 0),
                new GradientStop(Color.FromArgb(0x60, 0, 0, 0), 0.42),
                new GradientStop(Color.FromArgb(0x24, 0, 0, 0), 0.74),
                new GradientStop(Color.FromArgb(0x00, 0, 0, 0), 1),
            }
            : new GradientStops
            {
                new GradientStop(Color.FromArgb(0x4E, 0, 0, 0), 0),
                new GradientStop(Color.FromArgb(0x38, 0, 0, 0), 0.38),
                new GradientStop(Color.FromArgb(0x18, 0, 0, 0), 0.70),
                new GradientStop(Color.FromArgb(0x00, 0, 0, 0), 1),
            };
        var brush = new RadialGradientBrush
        {
            GradientOrigin = new RelativePoint(p.X / w, p.Y / h, RelativeUnit.Relative),
            Center = new RelativePoint(p.X / w, p.Y / h, RelativeUnit.Relative),
            RadiusX = new RelativeScalar(SoftBodyModel.DimpleSigma * 5.5 / w, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(SoftBodyModel.DimpleSigma * 5.5 / h, RelativeUnit.Relative),
        };
        foreach (GradientStop stop in stops) brush.GradientStops.Add(stop);
        dip.Background = brush;
        dip.Opacity = amount;
    }

    /// <summary>
    /// The projective map that carries the face, its label and its legend strip
    /// together. Each corner is displaced by the model, projected with a weak
    /// perspective, and a homography is solved through the four correspondences
    /// so everything between them follows.
    /// </summary>
    public static Matrix Homography(Point press, Size size, double amount)
    {
        Point[] src =
        {
            new(0, 0), new(size.Width, 0),
            new(size.Width, size.Height), new(0, size.Height),
        };
        var dst = new Point[4];
        var centre = new Point(size.Width / 2, size.Height / 2);
        for (int i = 0; i < 4; i++)
        {
            // sample just inside the rim: the corner itself is pinned to zero by
            // the skirt, so reading the model AT the corner would always give a
            // flat face. This is the cheap path's honest compromise - the
            // silhouette cannot move, so we take the depth the rubber reaches
            // a skirt-width in and let the corner carry it.
            double t = SoftBodyModel.SkirtWidth / Math.Max(1, Distance(src[i], centre));
            var probe = new Point(
                src[i].X + (centre.X - src[i].X) * t,
                src[i].Y + (centre.Y - src[i].Y) * t);
            double z = SoftBodyModel.Displacement(press, probe, size, amount);
            double shrink = 1 - z * Perspective;
            dst[i] = new Point(
                centre.X + (src[i].X - centre.X) * shrink,
                centre.Y + (src[i].Y - centre.Y) * shrink + z * Drop);
        }
        return Solve(src, dst);
    }

    private static double Distance(Point a, Point b)
    {
        double dx = a.X - b.X, dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>Solve the 8 unknowns of a homography from four point pairs.
    /// Avalonia's Matrix is row-vector: x' = (x*M11 + y*M21 + M31) / (x*M13 + y*M23 + M33).</summary>
    private static Matrix Solve(Point[] s, Point[] d)
    {
        var a = new double[8, 9];
        for (int i = 0; i < 4; i++)
        {
            double x = s[i].X, y = s[i].Y, u = d[i].X, v = d[i].Y;
            a[i * 2, 0] = x; a[i * 2, 1] = y; a[i * 2, 2] = 1;
            a[i * 2, 6] = -u * x; a[i * 2, 7] = -u * y; a[i * 2, 8] = u;
            a[i * 2 + 1, 3] = x; a[i * 2 + 1, 4] = y; a[i * 2 + 1, 5] = 1;
            a[i * 2 + 1, 6] = -v * x; a[i * 2 + 1, 7] = -v * y; a[i * 2 + 1, 8] = v;
        }
        for (int col = 0; col < 8; col++)
        {
            int pivot = col;
            for (int r = col + 1; r < 8; r++)
                if (Math.Abs(a[r, col]) > Math.Abs(a[pivot, col])) pivot = r;
            if (Math.Abs(a[pivot, col]) < 1e-12) return Matrix.Identity;
            if (pivot != col)
                for (int c = 0; c <= 8; c++)
                    (a[col, c], a[pivot, c]) = (a[pivot, c], a[col, c]);
            double p = a[col, col];
            for (int c = col; c <= 8; c++) a[col, c] /= p;
            for (int r = 0; r < 8; r++)
            {
                if (r == col) continue;
                double f = a[r, col];
                if (f == 0) continue;
                for (int c = col; c <= 8; c++) a[r, c] -= f * a[col, c];
            }
        }
        return new Matrix(
            a[0, 8], a[3, 8], a[6, 8],
            a[1, 8], a[4, 8], a[7, 8],
            a[2, 8], a[5, 8], 1);
    }
}
