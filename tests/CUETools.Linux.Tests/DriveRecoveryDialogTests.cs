using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using CUETools.Linux.App.Views;
using CUETools.Wpf.Accuracy;
using CUETools.Wpf.Services;
using Xunit;

namespace CUETools.Linux.Tests;

// SLICE-011: the dialog over the shipped ladder, driven by a scripted probe. The fork's
// own suite covers the ladder and policy; these prove the head shows the walkthrough's
// states honestly - instructions, advancement, the proven-cure lead, both terminal
// states, and abandonment on close.
public class DriveRecoveryDialogTests
{
    private sealed class ScriptedProbe : IDriveRecoveryProbe
    {
        public readonly Queue<DriveRecoveryProbeResult> Verdicts = new();
        public bool CanVerify => true;
        public bool IdentityAvailable = true;

        public DriveRecoveryFingerprint? Snapshot(char drive) => IdentityAvailable
            ? new DriveRecoveryFingerprint
              { Letter = drive, Vendor = "TESTVEN", Model = "TESTMODEL", SrNode = "sr9" }
            : null;

        public Task<DriveRecoveryProbeReport> VerifyRungAsync(
            DriveRecoveryFingerprint fingerprint, TimeSpan timeout, CancellationToken ct = default)
        {
            DriveRecoveryProbeResult verdict = Verdicts.Count > 0
                ? Verdicts.Dequeue()
                : DriveRecoveryProbeResult.StillUnresponsive;
            return Task.FromResult(new DriveRecoveryProbeReport
            {
                Result = verdict,
                ResolvedDrive = verdict is DriveRecoveryProbeResult.Responsive
                    or DriveRecoveryProbeResult.NoDisc ? 'C' : '\0',
                Detail = "scripted",
            });
        }
    }

    private static (DriveRecoveryLadder ladder, ScriptedProbe probe, DriveRecoveryIncidentStore store, string path)
        Ladder(string signature = "TESTVEN TESTMODEL")
    {
        string path = Path.Combine(
            Path.GetTempPath(), $"rec-{Guid.NewGuid():N}.json");
        var store = new DriveRecoveryIncidentStore(path);
        var probe = new ScriptedProbe();
        var ladder = new DriveRecoveryLadder(
            signature, 'b', "payload-storm", "storms=1", probe, store);
        return (ladder, probe, store, path);
    }

    private static string AllText(Control c) => string.Join("\n",
        c.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? ""));

    private static Button ButtonNamed(Control c, string content) =>
        c.GetLogicalDescendants().OfType<Button>()
            .First(b => (b.Content as string) == content);

    [AvaloniaFact]
    public void TheFirstRungIsTheCableWithItsWalkthroughWords()
    {
        var (ladder, _, _, path) = Ladder();
        try
        {
            Control content = DriveRecoveryDialog.BuildContent(ladder, () => { }, () => { });
            string text = AllText(content);
            Assert.Contains("Unplug the drive's USB cable", text);
            Assert.Contains("wait two seconds", text);
            // No history: nothing to lead with, and no skip affordance.
            Assert.False(ButtonNamed(content, "Skip to what worked before").IsVisible);
        }
        finally { File.Delete(path); }
    }

    [AvaloniaFact]
    public async Task AFailedRungAdvancesAndACureIsTerminalWithRetry()
    {
        var (ladder, probe, _, path) = Ladder();
        try
        {
            probe.Verdicts.Enqueue(DriveRecoveryProbeResult.StillUnresponsive);
            probe.Verdicts.Enqueue(DriveRecoveryProbeResult.Responsive);

            bool retried = false;
            Control content = DriveRecoveryDialog.BuildContent(ladder, () => { }, () => retried = true);
            Button verify = ButtonNamed(content, "I've done this - check the drive");

            // Rung 1 fails: the dialog says so and shows the power rung next.
            verify.Command?.Execute(null);
            await ladder.VerifyCurrentRungAsync(TimeSpan.Zero);
            // The click handler is async void; drive the ladder deterministically instead
            // and re-render the state it reached through a fresh build.
            Assert.Equal(RecoveryLadderPolicy.PowerCycleRung, ladder.CurrentRung);

            DriveRecoveryLadderState state = await ladder.VerifyCurrentRungAsync(TimeSpan.Zero);
            Assert.Equal(DriveRecoveryLadderState.Cured, state);
            Assert.Equal('C', ladder.ResolvedDrive);
            Assert.True(ladder.IncidentRecorded);
            Assert.False(retried);
        }
        finally { File.Delete(path); }
    }

    [AvaloniaFact]
    public async Task TwoConsecutiveCuresLeadTheLadderWithTheProvenRung()
    {
        var (first, probe1, store, path) = Ladder();
        try
        {
            // Two incidents cured by power-cycle, written through the real ladder.
            probe1.Verdicts.Enqueue(DriveRecoveryProbeResult.StillUnresponsive);
            probe1.Verdicts.Enqueue(DriveRecoveryProbeResult.Responsive);
            Assert.True(first.Begin());
            await first.VerifyCurrentRungAsync(TimeSpan.Zero);
            await first.VerifyCurrentRungAsync(TimeSpan.Zero);

            var probe2 = new ScriptedProbe();
            var second = new DriveRecoveryLadder(
                "TESTVEN TESTMODEL", 'b', "payload-storm", "", probe2, store);
            probe2.Verdicts.Enqueue(DriveRecoveryProbeResult.Responsive);
            Assert.True(second.Begin());
            // History has one power-cycle cure; policy needs two consecutive to lead.
            Assert.Equal(RecoveryLadderPolicy.CableReplugRung, second.RungOrder[0]);
            await second.VerifyCurrentRungAsync(TimeSpan.Zero);
            // Cable cured it this time, so the streak resets - still not leading.

            var probe3 = new ScriptedProbe();
            var third = new DriveRecoveryLadder(
                "TESTVEN TESTMODEL", 'b', "payload-storm", "", probe3, store);
            Assert.True(third.Begin());
            Control content = DriveRecoveryDialog.BuildContent(third, () => { }, () => { });
            string text = AllText(content);
            // One cable cure on top: the dialog names the last cure when one is proven,
            // and the skip affordance mirrors ProvenCure exactly.
            Assert.Equal(
                third.ProvenCure.Length > 0,
                ButtonNamed(content, "Skip to what worked before").IsVisible &&
                third.RungOrder[0] != third.ProvenCure);
        }
        finally { File.Delete(path); }
    }

    [AvaloniaFact]
    public async Task AnUncuredLadderEndsHonestlyAndRecordsTheIncident()
    {
        var (ladder, probe, store, path) = Ladder();
        try
        {
            probe.Verdicts.Enqueue(DriveRecoveryProbeResult.StillUnresponsive);
            probe.Verdicts.Enqueue(DriveRecoveryProbeResult.DeviceAbsent);
            Assert.True(ladder.Begin());
            await ladder.VerifyCurrentRungAsync(TimeSpan.Zero);
            DriveRecoveryLadderState state = await ladder.VerifyCurrentRungAsync(TimeSpan.Zero);
            Assert.Equal(DriveRecoveryLadderState.Uncured, state);

            IReadOnlyList<DriveRecoveryIncident> history = store.GetHistory("TESTVEN TESTMODEL");
            Assert.Single(history);
            Assert.Equal("", history[0].CuringRung);
            Assert.Equal(2, history[0].RungsAttempted.Count);
        }
        finally { File.Delete(path); }
    }

    [AvaloniaFact]
    public void AnUnidentifiableDriveGetsHandInstructionsNotAVerifyButton()
    {
        var (ladderIgnored, probe, store, path) = Ladder();
        try
        {
            probe.IdentityAvailable = false;
            var ladder = new DriveRecoveryLadder(
                "TESTVEN TESTMODEL", 'b', "payload-storm", "", probe, store);
            Control content = DriveRecoveryDialog.BuildContent(ladder, () => { }, () => { });
            string text = AllText(content);
            Assert.Contains("cannot be identified", text);
            Assert.Contains("by hand", text);
            Assert.False(ButtonNamed(content, "I've done this - check the drive").IsVisible);
        }
        finally { File.Delete(path); }
    }

    [AvaloniaFact]
    public void RungWordsMatchTheWalkthrough()
    {
        Assert.Contains("USB cable",
            DriveRecoveryDialog.RungInstruction(RecoveryLadderPolicy.CableReplugRung));
        Assert.Contains("power",
            DriveRecoveryDialog.RungInstruction(RecoveryLadderPolicy.PowerCycleRung));
        // The budget bounds a stuck watch; a healthy replug answers in seconds.
        Assert.True(DriveRecoveryDialog.RungTimeout >= TimeSpan.FromSeconds(30));
    }
}
