#if RIP_DIAGNOSTIC
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;
using CUETools.Wpf.Services;

namespace CUETools.Linux.App.Services;

/// <summary>
/// Dev-only preview of the CTDB submission consent dialog (SLICE-012).
///
/// Shows the real dialog, built by the real code, and prints the answer. There is no
/// CtdbSubmissionService anywhere in this path, so Share cannot upload anything: the
/// point is to review wording and layout on a real display without arming a button
/// wired to a live database. Reviewing it through a genuine verify would do exactly
/// that, which is too much risk for a look at the text.
///
/// The whole file is compiled out of Release builds; the flag does not exist there.
/// </summary>
internal static class CtdbConsentPreview
{
    internal static int Run()
    {
        AppBuilder.Configure<PreviewApp>()
            .UsePlatformDetect()
            .SetupWithLifetime(new ClassicDesktopStyleApplicationLifetime());

        // Sample values, not a real disc. Barcode is the printed one from Aja, chosen so
        // the barcode line renders at a realistic width.
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

        var submit = false;
        share.Click += (_, _) => { submit = true; dialog.Close(); };
        decline.Click += (_, _) => dialog.Close();

        Console.WriteLine("ctdb-consent-preview: showing the dialog. Nothing can be uploaded.");
        dialog.Show();

        var closed = false;
        dialog.Closed += (_, _) => closed = true;
        while (!closed)
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        Console.WriteLine(
            $"ctdb-consent-preview: answer submit={submit} remember={remember.IsChecked == true} " +
            "(no submission service exists in this path)");
        return 0;
    }

    /// <summary>Minimal application so the dialog picks up the app's own styles.</summary>
    private sealed class PreviewApp : Application
    {
        public override void Initialize()
        {
            Styles.Add(new FluentTheme());
            var palette = new ResourceInclude(new Uri("avares://CUETools.Linux.App/"))
            {
                Source = new Uri("avares://CUETools.Linux.App/Theme/Palette.axaml"),
            };
            Resources.MergedDictionaries.Add(palette);
        }
    }
}
#endif
