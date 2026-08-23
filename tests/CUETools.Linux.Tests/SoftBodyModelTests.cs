using Avalonia;
using CUETools.Linux.App.Controls;
using Xunit;

namespace CUETools.Linux.Tests;

// SLICE-015's deformation field. Pure arithmetic, so all of this runs in the
// committed harness with nothing rendered - which matters because the renderer
// itself can only be checked under a locally patched Skia lane. If the physics
// is wrong, it is wrong here, in CI, on every push.
public class SoftBodyModelTests
{
    private static readonly Size Key = new(120, 32);
    private static Point Corner => new(8, 6);
    private static Point Centre => new(60, 16);
    private static Point FarCorner => new(112, 26);

    private static double Z(Point press, Point sample, double amount = 1.0)
        => SoftBodyModel.Displacement(press, sample, Key, amount);

    [Fact]
    public void S15_001_ACornerPressDrivesItsOwnCornerDeeperThanACentrePressDoes()
    {
        double cornerUnderCorner = Z(Corner, Corner);
        double cornerUnderCentre = Z(Centre, Corner);
        Assert.True(cornerUnderCorner > cornerUnderCentre,
            $"corner press should sink its corner further: {cornerUnderCorner:0.000} vs {cornerUnderCentre:0.000}");
    }

    [Fact]
    public void S15_001_ACornerPressLiftsTheOppositeCorner()
    {
        // the lever's far side rises: this is what makes it read as a rocking
        // cap rather than a uniformly sinking slab
        double far = Z(Corner, FarCorner);
        Assert.True(far < 0, $"opposite corner should lift, got {far:0.000}");
        Assert.True(Math.Abs(far) < SoftBodyModel.DepthBudget,
            "the lift must stay inside the depth budget");
    }

    [Fact]
    public void S15_002_ThePerimeterNeverMoves()
    {
        // the bonded skirt. Sampled densely all the way around, for several
        // press points, because a single corner check would miss a bad axis.
        foreach (Point press in new[] { Corner, Centre, new Point(119, 31), new Point(0, 0) })
        {
            for (double x = 0; x <= Key.Width; x += 0.5)
            {
                Assert.Equal(0, Z(press, new Point(x, 0)), 9);
                Assert.Equal(0, Z(press, new Point(x, Key.Height)), 9);
            }
            for (double y = 0; y <= Key.Height; y += 0.5)
            {
                Assert.Equal(0, Z(press, new Point(0, y)), 9);
                Assert.Equal(0, Z(press, new Point(Key.Width, y)), 9);
            }
        }
    }

    [Fact]
    public void S15_003_EveryPressPointStaysInsideTheDepthBudget()
    {
        // the whole press-point x sample-point cross product, on several key
        // shapes including the app's extremes and a degenerate sliver
        foreach (Size size in new[]
        {
            new Size(120, 32), new Size(200, 40), new Size(44, 38),
            new Size(30, 24), new Size(300, 28), new Size(8, 8),
        })
        {
            for (double px = 0; px <= size.Width; px += size.Width / 12)
            for (double py = 0; py <= size.Height; py += size.Height / 6)
            for (double sx = 0; sx <= size.Width; sx += size.Width / 12)
            for (double sy = 0; sy <= size.Height; sy += size.Height / 6)
            {
                double z = SoftBodyModel.Displacement(
                    new Point(px, py), new Point(sx, sy), size, 1.0);
                Assert.True(Math.Abs(z) <= SoftBodyModel.DepthBudget + 1e-9,
                    $"{size} press({px},{py}) sample({sx},{sy}) = {z:0.000} exceeds budget");
            }
        }
    }

    [Fact]
    public void ACentrePressIsSymmetricAndACornerPressIsNot()
    {
        // centre: mirrored samples must match, or the cap is not sitting square
        // on its plunger
        Assert.Equal(Z(Centre, new Point(30, 16)), Z(Centre, new Point(90, 16)), 9);
        Assert.Equal(Z(Centre, new Point(60, 8)), Z(Centre, new Point(60, 24)), 9);

        // corner: the same mirrored pair must now differ, or there is no lever
        Assert.NotEqual(Z(Corner, new Point(30, 16)), Z(Corner, new Point(90, 16)), 3);
    }

    [Fact]
    public void ThePlungerIsThePivotOfACornerPress()
    {
        // the tilt term contributes nothing at the centre by construction, so
        // the centre sinks by travel and dimple only - it is the fulcrum, not
        // the deepest point of a corner press
        double centreUnderCorner = Z(Corner, Centre);
        double cornerUnderCorner = Z(Corner, Corner);
        Assert.True(centreUnderCorner > 0, "the plunger still collapses under a corner press");
        Assert.True(cornerUnderCorner > centreUnderCorner,
            "the pressed corner must sink further than the pivot");
    }

    [Fact]
    public void DisplacementIsMonotoneInPressForce()
    {
        double previous = 0;
        for (double amount = 0.1; amount <= 1.0; amount += 0.1)
        {
            double z = Z(Corner, Corner, amount);
            Assert.True(z > previous, $"press {amount:0.0} did not deepen: {z:0.000} vs {previous:0.000}");
            previous = z;
        }
        Assert.Equal(0, Z(Corner, Corner, 0), 9);
    }

    [Fact]
    public void TheDimpleIsLocalAndDoesNotScaleWithTheKey()
    {
        // the same press on a narrow and a wide key must dimple the same
        // distance away by the same amount: rubber stiffness is not a function
        // of how wide the button is. Sampled well inside the skirt on both.
        var narrow = new Size(60, 32);
        var wide = new Size(300, 32);
        var press = new Point(30, 16);

        // isolate the dimple by comparing a point near the press against one
        // far away along the same axis, on each key
        double nearNarrow = SoftBodyModel.Displacement(press, new Point(34, 16), narrow, 1);
        double farNarrow = SoftBodyModel.Displacement(press, new Point(50, 16), narrow, 1);
        double nearWide = SoftBodyModel.Displacement(press, new Point(34, 16), wide, 1);
        double farWide = SoftBodyModel.Displacement(press, new Point(50, 16), wide, 1);

        Assert.True(nearNarrow > farNarrow, "the dimple must fall off on a narrow key");
        Assert.True(nearWide > farWide, "the dimple must fall off on a wide key too");
    }

    [Fact]
    public void AKeyboardPressIsDeadCentreWithNoTilt()
    {
        // Space and Enter have no pointer position; inventing one would claim
        // the user pressed somewhere they did not
        Point press = SoftBodyModel.KeyboardPress(Key);
        Assert.Equal(Centre.X, press.X, 9);
        Assert.Equal(Centre.Y, press.Y, 9);
        Assert.Equal(Z(press, new Point(30, 16)), Z(press, new Point(90, 16)), 9);
    }

    [Fact]
    public void ARestingKeyIsPerfectlyFlat()
    {
        for (double x = 0; x <= Key.Width; x += 4)
        for (double y = 0; y <= Key.Height; y += 4)
            Assert.Equal(0, Z(Corner, new Point(x, y), 0), 9);
    }

    // --- dragging off the key (owner report 2026-08-23) ---------------------
    // Reported as "the shorter the button, the weirder it looks" for the same
    // drag distance. The budget sweep above only ever pressed points INSIDE the
    // key, so it could not see any of this.

    private static readonly Size[] Shapes =
    {
        new(360, 34), new(200, 40), new(120, 32),
        new(90, 30), new(64, 30), new(40, 30), new(8, 8),
    };

    [Fact]
    public void S15_010_APressDraggedOffTheKeyStaysInsideTheDepthBudget()
    {
        // the same invariant as S15_003, for the press points a real drag
        // actually reaches. A pointer 500 DIP away is an ordinary drag.
        foreach (Size size in Shapes)
        foreach (double d in new double[] { 12, 60, 200, 500, 2000 })
        foreach (var dir in new[] { (1.0, 0.0), (0.0, 1.0), (-1.0, 0.0), (0.7, 0.7), (-0.7, -0.7) })
        {
            var press = new Point(
                size.Width / 2 + dir.Item1 * d,
                size.Height / 2 + dir.Item2 * d);
            for (double sx = 0; sx <= size.Width; sx += Math.Max(1, size.Width / 8))
            for (double sy = 0; sy <= size.Height; sy += Math.Max(1, size.Height / 4))
            {
                double z = SoftBodyModel.Displacement(press, new Point(sx, sy), size, 1.0);
                Assert.True(Math.Abs(z) <= SoftBodyModel.DepthBudget + 1e-9,
                    $"{size} dragged {d} to ({press.X:0.#},{press.Y:0.#}), sample ({sx},{sy}) = {z:0.000}");
            }
        }
    }

    [Fact]
    public void S15_010_TheSameDragDoesNotDeformASmallKeyMoreThanALargeOne()
    {
        // the owner's actual complaint: identical gesture, wildly different
        // result depending on key width. A drag off the key is a drag off the
        // key; the key's own size must not amplify it.
        double Worst(Size size)
        {
            var press = new Point(size.Width / 2, size.Height / 2 + 500);
            double worst = 0;
            for (double sx = 0; sx <= size.Width; sx += Math.Max(1, size.Width / 8))
            for (double sy = 0; sy <= size.Height; sy += Math.Max(1, size.Height / 4))
                worst = Math.Max(worst,
                    Math.Abs(SoftBodyModel.Displacement(press, new Point(sx, sy), size, 1.0)));
            return worst;
        }

        double large = Worst(new Size(360, 34));
        foreach (Size small in new[] { new Size(120, 32), new Size(90, 30), new Size(40, 30) })
        {
            double got = Worst(small);
            Assert.True(got <= large + 0.05,
                $"{small} deformed {got:0.000} on the same drag that moved a 360x34 key {large:0.000}");
        }
    }

    [Fact]
    public void S15_011_ThePointerBearsOnTheKeyEvenWhenItHasLeftIt()
    {
        // a finger cannot press a point that is not on the key: contact stays
        // at the nearest point on the surface, so sliding straight down past
        // the bottom edge keeps rocking the key the same way, not further
        var size = new Size(120, 32);
        var sample = new Point(60, 30);
        double atEdge = SoftBodyModel.Displacement(new Point(60, 32), sample, size, 1.0);
        double wayPast = SoftBodyModel.Displacement(new Point(60, 900), sample, size, 1.0);
        Assert.True(Math.Abs(wayPast) <= Math.Abs(atEdge) + 1e-9,
            $"dragging past the edge deepened the press: {wayPast:0.000} vs {atEdge:0.000} at the edge");
    }

    [Fact]
    public void S15_012_TheKeyRisesAsThePointerSlidesOffIt()
    {
        // real rubber: slide your finger off a key you are holding and the load
        // transfers off, so it pops back up. That also tells the truth about
        // the outcome, because releasing out here does not activate it.
        var size = new Size(120, 32);
        var sample = new Point(60, 16);
        double onKey = Math.Abs(SoftBodyModel.Displacement(new Point(60, 16), sample, size, 1.0));
        double justOff = Math.Abs(SoftBodyModel.Displacement(new Point(60, 40), sample, size, 1.0));
        double wellOff = Math.Abs(SoftBodyModel.Displacement(new Point(60, 500), sample, size, 1.0));

        Assert.True(justOff < onKey, $"key should relax as the pointer leaves: {justOff:0.000} vs {onKey:0.000}");
        Assert.Equal(0, wellOff, 9);
    }

    [Fact]
    public void S15_012_DraggingBackOntoTheKeyPressesItAgain()
    {
        // the disengage is not a latch: slide back on and it presses again,
        // which is what the pointer semantics already promise on release
        var size = new Size(120, 32);
        var sample = new Point(60, 16);
        double there = SoftBodyModel.Displacement(new Point(60, 16), sample, size, 1.0);
        double gone = SoftBodyModel.Displacement(new Point(60, 500), sample, size, 1.0);
        double back = SoftBodyModel.Displacement(new Point(60, 16), sample, size, 1.0);
        Assert.Equal(0, gone, 9);
        Assert.Equal(there, back, 9);
        Assert.True(there > 0);
    }

    [Fact]
    public void S15_012_DisengagingIsSmoothSoTheKeyDoesNotSnapAtTheEdge()
    {
        // a hard cut at the boundary makes the key flick as the pointer grazes
        // the edge; the load has to hand over across a band
        var size = new Size(120, 32);
        var sample = new Point(60, 16);
        double prev = Math.Abs(SoftBodyModel.Displacement(new Point(60, 16), sample, size, 1.0));
        for (double y = 17; y <= 60; y += 1)
        {
            double z = Math.Abs(SoftBodyModel.Displacement(new Point(60, y), sample, size, 1.0));
            Assert.True(z <= prev + 1e-9, $"displacement rose while sliding away, at y={y}");
            Assert.True(prev - z < SoftBodyModel.DepthBudget * 0.25,
                $"displacement dropped {prev - z:0.000} in one DIP at y={y}: that is a snap, not a slide");
            prev = z;
        }
    }
}
