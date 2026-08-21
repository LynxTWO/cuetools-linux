using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using CUETools.Wpf.Services;

namespace CUETools.Linux.App.Services;

/// <summary>
/// The CTDB submission consent dialog (SLICE-012 step 1, D-069).
///
/// Until this existed no head implemented <see cref="ICtdbSubmissionPrompt"/>, and the
/// service treats a missing prompt as a refusal, so nothing could upload. That default is
/// preserved in every failure path here: no owner window, a closed dialog, a window-close
/// button, or an exception all leave <c>Submit</c> false.
///
/// The dialog names what leaves the machine rather than describing it as "your rip". A user
/// consenting to an upload has to be able to see what the upload contains, including the
/// per-machine identifier the CTDB client sends, which is the item nobody would guess.
/// </summary>
public sealed class AvaloniaCtdbSubmissionPrompt : ICtdbSubmissionPrompt
{
    private readonly Func<Window?> _windowSource;

    public AvaloniaCtdbSubmissionPrompt(Func<Window?> windowSource)
        => _windowSource = windowSource ?? throw new ArgumentNullException(nameof(windowSource));

    /// <summary>
    /// Called from the worker thread that ran the verify or rip. The dialog must live on
    /// the UI thread, and the UI thread must keep pumping while it is open, so the WORKER
    /// blocks here on a completion source while the UI thread awaits ShowDialog normally.
    ///
    /// The first version marshalled onto the UI thread and then blocked it with
    /// ShowDialog(owner).GetAwaiter().GetResult(). Avalonia's ShowDialog has no nested
    /// message pump (unlike WPF), so that deadlocked the renderer: the window was created
    /// and mapped but never painted - observed live as a title bar over transparent
    /// nothing, with the whole app frozen behind it, stuck exactly at the offer.
    /// </summary>
    public CtdbSubmissionConsent Ask(CtdbSubmissionCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        // On the UI thread there is no safe way to wait for an answer synchronously:
        // blocking deadlocks the renderer, exactly the bug above. Every real offer comes
        // from a Task.Run worker, so this branch is a guard, and it refuses rather than
        // freezes. A prompt that cannot be shown safely is not consent.
        if (Dispatcher.UIThread.CheckAccess())
            return new CtdbSubmissionConsent { Submit = false, Remember = false };

        var done = new TaskCompletionSource<CtdbSubmissionConsent>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(() => _ = RunDialogAsync(candidate, done));
        try
        {
            return done.Task.GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            return new CtdbSubmissionConsent { Submit = false, Remember = false };
        }
    }

    /// <summary>UI-thread side: build, show, await, and report. Every failure path
    /// resolves the completion source with a refusal so the worker can never hang.</summary>
    private async Task RunDialogAsync(
        CtdbSubmissionCandidate candidate,
        TaskCompletionSource<CtdbSubmissionConsent> done)
    {
        try
        {
            if (_windowSource() is not { } owner)
            {
                done.TrySetResult(new CtdbSubmissionConsent { Submit = false, Remember = false });
                return;
            }

            Window dialog = BuildDialog(
                candidate, out CheckBox remember, out Button yes, out Button no);

            var submit = false;
            // Wired here rather than in BuildDialog, so the builder stays pure layout
            // that a test can construct and read without a window ever being shown.
            yes.Click += (_, _) => { submit = true; dialog.Close(); };
            no.Click += (_, _) => dialog.Close();

            await dialog.ShowDialog(owner);

            done.TrySetResult(new CtdbSubmissionConsent
            {
                Submit = submit,
                Remember = remember.IsChecked == true,
            });
        }
        catch (Exception)
        {
            done.TrySetResult(new CtdbSubmissionConsent { Submit = false, Remember = false });
        }
    }

    /// <summary>
    /// Builds the dialog without showing it, so its wording and its defaults are testable.
    /// Pure layout: nothing here closes a window or records an answer.
    /// </summary>
    internal static Window BuildDialog(
        CtdbSubmissionCandidate candidate,
        out CheckBox remember,
        out Button share,
        out Button decline)
    {
        remember = new CheckBox
        {
            Content = "Remember this answer and stop asking",
            IsChecked = false,
        };

        var dialog = new Window
        {
            Title = "Share this rip with the CUETools Database?",
            SizeToContent = SizeToContent.WidthAndHeight,
            MaxWidth = 560,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };

        // Two separate decisions here, and they deliberately point different ways.
        //
        // Share carries the accent, because it is the action the dialog is asking about
        // and the database this feature exists to contribute to. Rendered without an
        // accent on either, the buttons were pixel-identical and nothing but the label
        // separated them (owner's call 2026-08-17 to put it here rather than on
        // declining).
        //
        // Declining stays the default and the cancel action, so Enter and Escape both
        // refuse. The emphasis invites; the keyboard still protects. A dialog that
        // appears under the user's hands must not read a reflex keypress as consent to
        // publish something that cannot be taken back.
        var yes = new Button
        {
            Content = "Share",
            MinWidth = 96,
            FontWeight = FontWeight.SemiBold,
        };
        var no = new Button
        {
            Content = "Don't share",
            MinWidth = 96,
            IsDefault = true,
            IsCancel = true,
        };
        // The accent key is an app-level style now (SLICE-014's machined key
        // theme), so a window built in code gets it by class like everything
        // else. The Share button keeps the accent by design: consent's yes is
        // the one lit key in the dialog.
        yes.Classes.Add("accent");
        share = yes;
        decline = no;

        string disc = string.IsNullOrWhiteSpace(candidate.Album)
            ? "this disc"
            : string.IsNullOrWhiteSpace(candidate.Artist)
                ? candidate.Album
                : candidate.Artist + " - " + candidate.Album;

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(24),
            Spacing = 14,
            Children =
            {
                new TextBlock
                {
                    Text = "This rip of " + disc + " verified cleanly. Sharing it helps " +
                           "other people check their own copies of the same disc.",
                    TextWrapping = TextWrapping.Wrap,
                },
                new TextBlock
                {
                    Text = "Sharing sends:",
                    TextWrapping = TextWrapping.Wrap,
                    FontWeight = FontWeight.SemiBold,
                },
                new TextBlock
                {
                    // Every line is something that actually goes out. The identifier is
                    // listed because a reader would not otherwise know it exists.
                    Text = BuildContentsList(candidate),
                    TextWrapping = TextWrapping.Wrap,
                },
                new TextBlock
                {
                    Text = "It does not send your audio files, your file names, or " +
                           "anything about where they are stored. Sharing cannot be " +
                           "undone: the database has no delete.",
                    TextWrapping = TextWrapping.Wrap,
                },
                remember,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    // Decline sits left of Share, so the destructive-by-default reading
                    // order puts the safe answer where the eye lands first.
                    Children = { no, yes },
                },
            },
        };

        return dialog;
    }

    private static string BuildContentsList(CtdbSubmissionCandidate candidate)
    {
        string barcode = string.IsNullOrWhiteSpace(candidate.Barcode)
            ? "the disc's barcode, if it has one"
            : "the disc's barcode (" + candidate.Barcode + ")";

        return
            "  - the disc's table of contents, which identifies the pressing\n" +
            "  - a checksum for each track\n" +
            "  - parity data, which is what lets the database repair a damaged copy\n" +
            "  - the artist and album title shown above\n" +
            "  - " + barcode + "\n" +
            // Kept short enough not to wrap at this width. A wrapped bullet returns to
            // the left margin rather than hanging under its own text, which reads as a
            // new item rather than a continuation.
            "  - an identifier for this computer, to tell submissions apart";
    }
}
