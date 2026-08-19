using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CUETools.Wpf.Accuracy;

namespace CUETools.Linux.App.Views;

/// <summary>
/// The guided drive recovery dialog (SLICE-011, D-060/D-061/D-062). The human
/// performs each physical rung; the app only instructs, verifies, and records.
///
/// Honesty rules carried from the brief: the dialog never claims a cure it did
/// not observe (the probe's verdict is the only source), it says when a cure
/// could not be remembered, an unreadable history is named rather than papered
/// over, and the terminal failure state offers real next steps instead of a
/// retry loop. Closing the window mid-ladder abandons it, which records the
/// incident as uncured rather than losing it.
///
/// Built in code like the consent dialog, so BuildContent is pure layout a test
/// can construct and read without a window.
/// </summary>
public sealed class DriveRecoveryDialog
{
    /// <summary>Per-rung verification budget. Replug re-enumeration measured in
    /// single-digit seconds on 2026-08-14; this bounds a stuck watch, it does not
    /// pace a healthy one - the probe returns the moment the drive answers.</summary>
    internal static readonly TimeSpan RungTimeout = TimeSpan.FromSeconds(45);

    internal static string RungInstruction(string rung) => rung switch
    {
        RecoveryLadderPolicy.CableReplugRung =>
            "Unplug the drive's USB cable, wait two seconds, and plug it back in.",
        RecoveryLadderPolicy.PowerCycleRung =>
            "Unplug the drive's power cord (or switch it off), wait two seconds, " +
            "and restore power.",
        _ => "Perform the step, then let CUETools check the drive.",
    };

    internal static string RungTitle(string rung) => rung switch
    {
        RecoveryLadderPolicy.CableReplugRung => "Reconnect the USB cable",
        RecoveryLadderPolicy.PowerCycleRung => "Cycle the drive's power",
        _ => "Recovery step",
    };

    /// <summary>Shows the dialog and drives the ladder to a terminal state.</summary>
    public static async Task ShowAsync(Window owner, DriveRecoveryLadder ladder, Action onRetry)
    {
        var dialog = new Window
        {
            Title = $"Recover drive {ladder.Drive}",
            SizeToContent = SizeToContent.WidthAndHeight,
            MaxWidth = 560,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };

        bool retryRequested = false;
        dialog.Content = BuildContent(
            ladder,
            close: () => dialog.Close(),
            retry: () => { retryRequested = true; dialog.Close(); });

        dialog.Closed += (_, _) =>
        {
            // A window closed mid-ladder is a dismissal: record it as uncured
            // rather than losing the incident (the ladder ignores this once
            // terminal, so a Cured close stays cured).
            if (!ladder.IsTerminal) ladder.Abandon();
        };

        await dialog.ShowDialog(owner);
        if (retryRequested) onRetry();
    }

    /// <summary>Pure layout over the ladder; all async work hangs off the buttons.</summary>
    internal static Control BuildContent(DriveRecoveryLadder ladder, Action close, Action retry)
    {
        var title = new TextBlock
        {
            FontWeight = FontWeight.SemiBold,
            FontSize = 15,
            TextWrapping = TextWrapping.Wrap,
        };
        var instruction = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var detail = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 11.5,
            Opacity = 0.75,
        };
        var historyLine = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 11.5,
            Opacity = 0.75,
        };

        var verify = new Button { Content = "I've done this - check the drive", MinWidth = 220 };
        var skip = new Button { Content = "Skip to what worked before", MinWidth = 180 };
        var retryNow = new Button { Content = "Retry now", MinWidth = 110, IsVisible = false };
        var closeBtn = new Button { Content = "Close", MinWidth = 90 };

        void ShowRung()
        {
            string? rung = ladder.CurrentRung;
            if (rung == null) return;
            title.Text = RungTitle(rung);
            instruction.Text = RungInstruction(rung);
            // The skip affordance exists only while the proven cure is still ahead
            // of the current rung (D-061): the full ladder stays reachable.
            skip.IsVisible =
                ladder.ProvenCure.Length > 0 &&
                rung != ladder.ProvenCure &&
                ladder.RungOrder.Contains(ladder.ProvenCure);
        }

        void ShowTerminal()
        {
            verify.IsVisible = false;
            skip.IsVisible = false;
            switch (ladder.State)
            {
                case DriveRecoveryLadderState.Cured:
                    title.Text = $"Drive {ladder.ResolvedDrive} answers again";
                    instruction.Text =
                        "The drive responded to a table-of-contents read. The failed " +
                        "run stays failed; Retry starts a fresh operation through the " +
                        "normal calibrated paths.";
                    detail.Text = ladder.IncidentRecorded
                        ? "This cure was recorded for this drive, so next time the " +
                          "dialog can lead with what worked."
                        : "This cure could not be recorded, so next time starts from " +
                          "the usual first step.";
                    retryNow.IsVisible = true;
                    break;
                case DriveRecoveryLadderState.Uncured:
                    title.Text = "The drive did not come back";
                    instruction.Text =
                        "Neither step brought the drive back. Try a different USB port " +
                        "or cable, or another machine; if it stays dead there, the " +
                        "drive may need service. Nothing about your discs or files was " +
                        "touched.";
                    detail.Text = ladder.IncidentRecorded
                        ? "The attempt was recorded as uncured."
                        : "";
                    break;
                case DriveRecoveryLadderState.PermissionsBlocked:
                    title.Text = "CUETools cannot check the drive";
                    instruction.Text =
                        "The device could not be opened for lack of permission, so no " +
                        "step was verified. Check that your user is in the cdrom group, " +
                        "then log out and back in.";
                    break;
            }
        }

        verify.Click += async (_, _) =>
        {
            verify.IsEnabled = false;
            skip.IsEnabled = false;
            string watched = title.Text ?? "";
            title.Text = "Checking the drive...";
            instruction.Text = "Watching for the drive to come back and answer a " +
                               "table-of-contents read.";
            DriveRecoveryLadderState state = await ladder.VerifyCurrentRungAsync(RungTimeout);
            verify.IsEnabled = true;
            skip.IsEnabled = true;
            detail.Text = ladder.LastDetail;
            if (ladder.IsTerminal)
            {
                ShowTerminal();
            }
            else
            {
                // Advanced: the rung did not cure it, and the next one is now current.
                ShowRung();
                instruction.Text =
                    "The drive did not answer after that step. Next: " + instruction.Text;
            }
        };

        skip.Click += (_, _) =>
        {
            if (ladder.SkipToRung(ladder.ProvenCure)) ShowRung();
        };

        retryNow.Click += (_, _) => retry();
        closeBtn.Click += (_, _) => close();

        if (!ladder.Begin())
        {
            title.Text = "This drive cannot be identified";
            instruction.Text =
                "CUETools could not capture the drive's identity, and without it a " +
                "returning drive cannot be told apart from its neighbours - a check " +
                "could report a cure for a drive that is still wedged. Perform the " +
                "usual physical steps by hand: unplug the USB cable and reconnect " +
                "it, then if the drive still does not respond, cut its power for a " +
                "few seconds.";
            verify.IsVisible = false;
            skip.IsVisible = false;
        }
        else
        {
            // A ladder handed over already terminal renders its verdict, not a rung the
            // user could pointlessly perform (caught by the render check, not a test).
            if (ladder.IsTerminal) ShowTerminal(); else ShowRung();
            historyLine.Text = ladder.HistoryUnreadable
                ? "This drive's recovery history could not be read; using the " +
                  "standard order."
                : ladder.ProvenCure.Length > 0
                    ? $"Last time, \"{RungTitle(ladder.ProvenCure)}\" cured this drive."
                    : "";
            historyLine.IsVisible = historyLine.Text.Length > 0;
        }

        return new StackPanel
        {
            Margin = new Avalonia.Thickness(24),
            Spacing = 12,
            MinWidth = 460,
            Children =
            {
                title,
                historyLine,
                instruction,
                detail,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { skip, verify, retryNow, closeBtn },
                },
            },
        };
    }
}
