using System;
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
    /// Called from the worker thread that ran the verify or rip, so the dialog is marshalled
    /// to the UI thread and this call blocks until the user answers. Blocking is correct
    /// here: the submission belongs to the run that produced it, and the live database
    /// object cannot be replayed later.
    /// </summary>
    public CtdbSubmissionConsent Ask(CtdbSubmissionCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        try
        {
            if (Dispatcher.UIThread.CheckAccess())
                return AskOnUiThread(candidate);

            return Dispatcher.UIThread.InvokeAsync(() => AskOnUiThread(candidate))
                .GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            // A prompt that cannot be shown is not consent. Never upload on an error path.
            return new CtdbSubmissionConsent { Submit = false, Remember = false };
        }
    }

    private CtdbSubmissionConsent AskOnUiThread(CtdbSubmissionCandidate candidate)
    {
        if (_windowSource() is not { } owner)
            return new CtdbSubmissionConsent { Submit = false, Remember = false };

        var submit = false;
        var remember = new CheckBox
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

        var yes = new Button { Content = "Share", MinWidth = 96 };
        var no = new Button { Content = "Don't share", MinWidth = 96, IsDefault = true };
        yes.Click += (_, _) => { submit = true; dialog.Close(); };
        no.Click += (_, _) => dialog.Close();

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
                    Children = { no, yes },
                },
            },
        };

        dialog.ShowDialog(owner).GetAwaiter().GetResult();

        return new CtdbSubmissionConsent
        {
            Submit = submit,
            Remember = remember.IsChecked == true,
        };
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
            "  - an identifier for this computer, which the database uses to tell " +
            "separate submissions apart";
    }
}
