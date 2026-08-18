using CUETools.Wpf.Models;
using CUETools.Wpf.Services;
using Xunit;

namespace CUETools.Linux.Tests;

// S10-006: the pure derivation behind the rip progress visuals, no hardware. Fraction math
// from TOC boundaries, phase transitions, per-track attribution, and the honesty rules the
// brief binds the display to (D-057, D-058).
public class TrackProgressModelTests
{
    private static TrackProgressModel Model(params double[] minutes)
    {
        var model = new TrackProgressModel();
        model.SetBoundaries(minutes.Select(m => TimeSpan.FromMinutes(m)).ToArray());
        return model;
    }

    private static RereadReport Reread(
        int reReads, double windowFrac, int givenUp = -1, int max = 16, int errors = 1)
        => new(reReads, max, errors, windowFrac, givenUp);

    [Fact]
    public void FractionsFollowTheTocProportions()
    {
        // 2 + 6 + 2 minutes: boundaries at 0.2 and 0.8.
        TrackProgressModel model = Model(2, 6, 2);
        model.StartJob(testAndCopy: false, "Secure", multiPass: true);

        model.OnProgress(0.1);
        Assert.Equal(0.5, model.Current[0], 3);   // halfway through track 1
        Assert.Equal(0, model.Current[1], 3);
        Assert.Equal(0, model.ActiveIndex);

        model.OnProgress(0.5);
        Assert.Equal(1, model.Current[0], 3);     // track 1 done
        Assert.Equal(0.5, model.Current[1], 3);   // halfway through track 2
        Assert.Equal(0, model.Current[2], 3);
        Assert.Equal(1, model.ActiveIndex);

        model.OnProgress(1);
        Assert.All(model.Current, f => Assert.Equal(1, f, 3));
        Assert.Equal(-1, model.ActiveIndex);
    }

    [Fact]
    public void SingleReadJobsHaveNoPhaseChip()
    {
        TrackProgressModel model = Model(3, 3);
        model.StartJob(testAndCopy: false, "Paranoid", multiPass: true);
        Assert.Equal(RipPhaseKind.SingleRead, model.Phase);
        Assert.Equal("", model.PhaseChip);
        Assert.Equal("PARANOID", model.ModeChip);
    }

    [Fact]
    public void TestAndCopyWalksThePhaseLadderOnTheNextProgressEvent()
    {
        TrackProgressModel model = Model(3, 3);
        model.StartJob(testAndCopy: true, "Secure", multiPass: true);
        Assert.Equal("TEST", model.PhaseChip);

        model.OnProgress(0.6);
        // The Test read finishes; its final frame keeps the TEST label.
        model.OnReadCompleted(0);
        Assert.Equal("TEST", model.PhaseChip);

        // The Copy read's first progress flips the phase and retains the Test outline.
        model.OnProgress(0.05);
        Assert.Equal("COPY", model.PhaseChip);
        Assert.Equal(1, model.TestRetained[0], 3);
        Assert.Equal(0.2, model.TestRetained[1], 3);
        Assert.True(model.Current[0] < 1);   // Copy fill restarted

        // A third read after Copy completes gets its own honest chip.
        model.OnReadCompleted(1);
        model.OnProgress(0.01);
        Assert.Equal("READ 3", model.PhaseChip);
    }

    [Fact]
    public void TheTestOutlineIsNeverRepaintedByCopyProgress()
    {
        TrackProgressModel model = Model(4, 4);
        model.StartJob(testAndCopy: true, "Secure", multiPass: true);
        model.OnProgress(0.3);            // Test reached 60% of track 1
        model.OnReadCompleted(0);
        model.OnProgress(0.9);            // Copy sweeps far past it
        Assert.Equal(0.6, model.TestRetained[0], 3);
        model.OnProgress(1.0);
        Assert.Equal(0.6, model.TestRetained[0], 3);   // D-057: retained, immutable
    }

    [Fact]
    public void PassTicksAreTheLiteralWindowPassCount()
    {
        TrackProgressModel model = Model(3, 3);
        model.StartJob(testAndCopy: false, "Secure", multiPass: true);

        model.OnReread(Reread(3, 0.1));
        Assert.Equal(3, model.PassTicks);
        Assert.Equal(16, model.PassMax);

        model.OnReread(Reread(7, 0.1));
        Assert.Equal(7, model.PassTicks);

        // The window converged: the lane goes quiet rather than freezing its last count.
        model.OnReread(Reread(0, 0.1));
        Assert.Equal(0, model.PassTicks);
    }

    [Fact]
    public void BurstShowsNoPassLane()
    {
        TrackProgressModel model = Model(3);
        model.StartJob(testAndCopy: false, "Burst", multiPass: false);
        // D-058: absence is the honest display of one-pass mode.
        Assert.False(model.PassLaneVisible);
    }

    [Fact]
    public void AWindowIsOneTickHoweverLongItGrinds()
    {
        TrackProgressModel model = Model(2, 2);
        model.StartJob(testAndCopy: false, "Secure", multiPass: true);

        // Thirty passes on one window inside track 1: one distinct window.
        for (int pass = 1; pass <= 30; pass++)
            model.OnReread(Reread(pass, 0.2));
        Assert.Equal(1, model.RereadWindows[0]);
        Assert.Equal(0, model.RereadWindows[1]);

        // A different window inside track 2.
        model.OnReread(Reread(1, 0.7));
        Assert.Equal(1, model.RereadWindows[1]);

        // Re-entering an earlier fraction with a RESET pass count is a new grind, not the
        // same window continuing.
        model.OnReread(Reread(1, 0.2));
        Assert.Equal(2, model.RereadWindows[0]);
    }

    [Fact]
    public void GivenUpSectorsAccumulateOnTheTrackThatOwnsTheWindow()
    {
        TrackProgressModel model = Model(2, 2);
        model.StartJob(testAndCopy: false, "Secure", multiPass: true);

        model.OnReread(Reread(30, 0.2, givenUp: 236));
        model.OnReread(Reread(30, 0.7, givenUp: 12));
        model.OnReread(Reread(30, 0.7, givenUp: 4));

        Assert.Equal(236, model.GivenUpSectors[0]);
        Assert.Equal(16, model.GivenUpSectors[1]);
    }

    [Fact]
    public void RoutineCorrectionsNeverMark()
    {
        TrackProgressModel model = Model(2);
        model.StartJob(testAndCopy: false, "Secure", multiPass: true);
        // A window that re-read and converged: amber tick territory, no red.
        model.OnReread(Reread(4, 0.5));
        model.OnReread(Reread(0, 0.5));
        Assert.Equal(0, model.GivenUpSectors[0]);
        Assert.Equal(1, model.RereadWindows[0]);
    }

    [Fact]
    public void TerminalMarksSurviveTheJobEnd()
    {
        TrackProgressModel model = Model(2, 2);
        model.StartJob(testAndCopy: true, "Salvage", multiPass: true);
        model.OnReread(Reread(30, 0.2, givenUp: 50));
        model.EndJob(completed: true);

        // S10-005: completion-with-unrecoverable, not a clean fill.
        Assert.Equal(50, model.GivenUpSectors[0]);
        Assert.All(model.Current, f => Assert.Equal(1, f, 3));
        Assert.Equal(RipPhaseKind.None, model.Phase);
        Assert.Equal(0, model.PassTicks);
    }

    [Fact]
    public void ANewJobClearsTheLastJobsMarks()
    {
        TrackProgressModel model = Model(2, 2);
        model.StartJob(testAndCopy: false, "Secure", multiPass: true);
        model.OnReread(Reread(5, 0.2, givenUp: 9));
        model.StartJob(testAndCopy: false, "Secure", multiPass: true);
        Assert.Equal(0, model.RereadWindows[0]);
        Assert.Equal(0, model.GivenUpSectors[0]);
    }

    [Fact]
    public void ImageLayoutSingleTrackStillDerives()
    {
        // Image + embedded CUE rips read as one span; a one-track boundary set must not
        // divide by zero or mis-attribute.
        TrackProgressModel model = Model(63.5);
        model.StartJob(testAndCopy: false, "Secure", multiPass: true);
        model.OnProgress(0.4);
        Assert.Equal(0.4, model.Current[0], 3);
        Assert.Equal(0, model.ActiveIndex);
        model.OnReread(Reread(2, 0.99, givenUp: 3));
        Assert.Equal(3, model.GivenUpSectors[0]);
    }

    [Fact]
    public void ZeroLengthBoundarySetIsInert()
    {
        var model = new TrackProgressModel();
        model.SetBoundaries(Array.Empty<TimeSpan>());
        model.StartJob(testAndCopy: false, "Secure", multiPass: true);
        model.OnProgress(0.5);
        model.OnReread(Reread(3, 0.5, givenUp: 7));
        Assert.Equal(-1, model.ActiveIndex);
        Assert.Empty(model.Current);
    }
}
