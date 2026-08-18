#if RIP_DIAGNOSTIC
using Avalonia.Controls;
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
            Console.WriteLine(
                $"ctdb-consent-preview: answer submit={submit} " +
                $"remember={remember.IsChecked == true} " +
                "(no submission service exists in this path)");
            dialog.Close();
        }

        share.Click += (_, _) => Answer(true);
        decline.Click += (_, _) => Answer(false);

        Console.WriteLine("ctdb-consent-preview: showing the dialog. Nothing can be uploaded.");
        return dialog;
    }
}
#endif
