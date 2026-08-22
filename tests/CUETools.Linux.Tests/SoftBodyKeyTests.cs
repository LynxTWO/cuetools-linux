using Avalonia;
using CUETools.Linux.App.Controls;
using Xunit;

namespace CUETools.Linux.Tests;

// The projective map that carries the face, label and legend strip together.
// Pure geometry, so it runs in the committed harness: if the homography is
// wrong the key shears like a card instead of rocking like a cap, and that is
// exactly the failure the WPF head hit and dialled back years ago.
public class SoftBodyKeyTests
{
    private static readonly Size KeySize = new(120, 32);
    private static Point Corner => new(8, 6);
    private static Point Centre => new(60, 16);

    private static Point Map(Matrix m, Point p)
    {
        double w = p.X * m.M13 + p.Y * m.M23 + m.M33;
        if (Math.Abs(w) < 1e-12) return p;
        return new Point(
            (p.X * m.M11 + p.Y * m.M21 + m.M31) / w,
            (p.X * m.M12 + p.Y * m.M22 + m.M32) / w);
    }

    [Fact]
    public void ARestingKeyIsNotTransformedAtAll()
    {
        Matrix m = SoftBodyKey.Homography(Centre, KeySize, 0);
        foreach (Point p in new[] { new Point(0, 0), new Point(120, 32), new Point(60, 16) })
        {
            Point q = Map(m, p);
            Assert.Equal(p.X, q.X, 6);
            Assert.Equal(p.Y, q.Y, 6);
        }
    }

    [Fact]
    public void TheHomographyIsSolvedExactlyThroughItsFourCorners()
    {
        // if the solve is wrong every interior pixel is wrong too, and nothing
        // downstream would tell us
        Matrix m = SoftBodyKey.Homography(Corner, KeySize, 1);
        Assert.NotEqual(Matrix.Identity, m);
        foreach (Point p in new[]
        {
            new Point(0, 0), new Point(120, 0), new Point(120, 32), new Point(0, 32),
        })
        {
            Point q = Map(m, p);
            Assert.True(Math.Abs(q.X - p.X) < 12 && Math.Abs(q.Y - p.Y) < 12,
                $"corner {p} moved to {q}, which is further than the depth budget allows");
        }
    }

    [Fact]
    public void ACornerPressCarriesItsOwnCornerFurtherThanTheOppositeOne()
    {
        Matrix m = SoftBodyKey.Homography(Corner, KeySize, 1);
        Point pressed = Map(m, new Point(0, 0));
        Point far = Map(m, new Point(120, 32));

        // the pressed corner sinks: on a bench lit from above, that reads as
        // moving DOWN the screen
        Assert.True(pressed.Y > 0, $"pressed corner should drop, went to {pressed.Y:0.000}");
        // the far corner rises, which is the lever and the whole point
        Assert.True(far.Y < 32, $"far corner should lift, went to {far.Y:0.000}");
    }

    [Fact]
    public void ACentrePressIsSymmetricSoTheCapDoesNotLean()
    {
        Matrix m = SoftBodyKey.Homography(Centre, KeySize, 1);
        Point tl = Map(m, new Point(0, 0));
        Point tr = Map(m, new Point(120, 0));
        Point bl = Map(m, new Point(0, 32));
        Point br = Map(m, new Point(120, 32));

        Assert.Equal(tl.Y, tr.Y, 6);
        Assert.Equal(bl.Y, br.Y, 6);
        Assert.Equal(60 - tl.X, tr.X - 60, 6);
    }

    [Fact]
    public void TheMapIsGenuinelyProjectiveAndNotAFlattenedAffine()
    {
        // D-084 trap 1: a TransformOperationsTransition silently drops the
        // perspective terms, and nothing errors. If that ever happens again the
        // cap tilts like a rigid card. This is the assertion that catches it.
        Matrix m = SoftBodyKey.Homography(Corner, KeySize, 1);
        bool projective = Math.Abs(m.M13) > 1e-9 || Math.Abs(m.M23) > 1e-9;
        Assert.True(projective,
            $"the map lost its perspective terms: M13={m.M13:0.000000} M23={m.M23:0.000000}");
    }

    [Fact]
    public void TheFlagIsOffUnlessTheEnvironmentAsksForIt()
    {
        // the gate build must never become the default by accident
        Assert.Equal(
            Environment.GetEnvironmentVariable("CUETOOLS_SOFTBODY") == "1",
            SoftBodyKey.Enabled);
    }
}
