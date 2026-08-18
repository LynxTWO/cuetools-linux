#if RIP_DIAGNOSTIC
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CUETools.Wpf.Services;

namespace CUETools.Linux.App.Services;

/// <summary>
/// Dev-only preview of the CTDB submission consent dialog (SLICE-012).
///
/// Returns the real dialog, built by the real code, for App to show as its main window.
/// No CtdbSubmissionService exists anywhere in this path, so Share cannot upload: the
/// point is to review wording and layout on a real display without arming a button wired
/// to a live database. Reviewing it through a genuine verify would do exactly that, which
/// is more risk than reading text should carry.
///
/// Shown as the main window rather than a modal on top of one, because the preview has no
/// application window to be modal to. A first attempt built its own AppBuilder and drove
/// the dispatcher with a RunJobs loop; that is not an event loop, and the window appeared
/// transparent and unpainted.
///
/// The answer is shown on screen, not only printed. The first version printed to stdout
/// alone, which is invisible when the process is launched detached, so clicking Share
/// looked exactly like the app doing nothing. That is the same complaint the real path
/// turned out to have, and a preview should not reproduce the bug it exists to check for.
///
/// The whole file is compiled out of Release builds; the flag does not exist there.
/// </summary>
internal static class CtdbConsentPreview
{
    internal static Window BuildWindow()
    {
        // Sample values, not a real disc. The barcode is Aja's printed one, chosen so the
        // barcode line renders at a realistic width.
        var candidate = new CtdbSubmissionCandidate
        {
            RunCompleted = true,
            Album = "Aja",
            Artist = "Steely Dan",
            Barcode = "0075992526227",
            Confidence = 4,
        };

        Window dialog = AvaloniaCtdbSubmissionPrompt.BuildDialog(
            candidate, out CheckBox remember, out Button share, out Button decline);

        void Answer(bool submit)
        {
            bool remembered = remember.IsChecked == true;
            Console.WriteLine(
                $"ctdb-consent-preview: answer submit={submit} remember={remembered} " +
                "(no submission service exists in this path)");
            ShowAnswer(dialog, submit, remembered);
        }

        share.Click += (_, _) => Answer(true);
        decline.Click += (_, _) => Answer(false);

        Console.WriteLine("ctdb-consent-preview: showing the dialog. Nothing can be uploaded.");
        return dialog;
    }

    /// <summary>
    /// Replaces the dialog's contents with what the answer was, rather than closing.
    /// Closing the main window ends the process, which is what made the first version look
    /// like nothing had happened.
    /// </summary>
    private static void ShowAnswer(Window dialog, bool submit, bool remembered)
    {
        var close = new Button { Content = "Close", MinWidth = 96, IsDefault = true };
        close.Click += (_, _) => dialog.Close();

        string headline = submit
            ? "You chose Share."
            : "You chose Don't share.";

        string effect = submit
            ? "In the real app this is the point where the upload happens, and the " +
              "Verify page's status line then reports whether it landed or failed."
            : "In the real app nothing is sent, and nothing is written to your settings.";

        string memory = remembered
            ? "You ticked remember, so the real app would store this answer and stop asking."
            : "You left remember unticked, so the real app would ask again for the next disc.";

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(24),
            Spacing = 14,
            Children =
            {
                new TextBlock
                {
                    Text = headline,
                    FontWeight = FontWeight.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                },
                new TextBlock { Text = effect, TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = memory, TextWrapping = TextWrapping.Wrap },
                new TextBlock
                {
                    Text = "This is a preview. Nothing was uploaded and nothing was saved.",
                    TextWrapping = TextWrapping.Wrap,
                    FontStyle = FontStyle.Italic,
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { close },
                },
            },
        };
    }
}
#endif
