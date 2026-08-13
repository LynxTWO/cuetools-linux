using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CUETools.Linux.App.Services;

namespace CUETools.Linux.App;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Composition.RegisterManagedCodecs();

            var theme = new ThemeState();
            theme.Apply(theme.Current);

            // --repair is explicit consent: confirmations auto-accept and
            // every repairable disc is repaired after verification. The
            // interactive prompt guards stray clicks, not scripted
            // instructions.
            bool autoRepair = desktop.Args?.Contains("--repair") == true;

            MainWindow? windowRef = null;
            Composition.AppGraph graph = Composition.CreateAppGraph(
                new AvaloniaFileDialogService(() => windowRef),
                autoRepair ? new AutoConfirmPrompt() : new AvaloniaUserPrompt(() => windowRef),
                new AvaloniaUiDispatcher());
            var verify = graph.Verify;
            if (autoRepair)
            {
                graph.Log.Info("repair", "--repair: confirmations auto-accepted by command-line consent");
                _ = new AutoRepairDriver(verify, line => graph.Log.Info("repair", line));
            }

            var window = new MainWindow(theme, verify);
            windowRef = window;
            window.Opened += (_, _) =>
            {
                // --smoke: prove the app reaches a visible window, then exit
                // (used by CI and startup measurements; the stopwatch starts
                // in Main so the number covers the whole launch).
                Console.WriteLine($"startup-to-window-ms={Program.Startup.ElapsedMilliseconds}");
                if (desktop.Args is ["--smoke", ..])
                {
                    // Hard exit by design: --smoke exists to prove the app
                    // reaches a visible window. Graceful lifetime shutdown
                    // from inside Opened races StartCore's own use of the
                    // window (observed NullReferenceException on the X11
                    // backend), and a diagnostics path has nothing to tear
                    // down gracefully.
                    Console.Out.Flush();
                    Environment.Exit(0);
                }

                // Existing paths on the command line load into Verify at
                // startup; --verify also starts the run. Useful for desktop
                // file-manager integration and headless evidence runs.
                string[] paths = (desktop.Args ?? Array.Empty<string>())
                    .Where(arg => File.Exists(arg) || Directory.Exists(arg))
                    .ToArray();
                if (paths.Length > 0 && verify.LoadSources(paths) &&
                    (desktop.Args!.Contains("--verify") || autoRepair))
                {
                    verify.VerifyCommand.Execute(null);
                }

                // Verification backfill replay (D-010/D-011): pending offline
                // journal entries re-verify automatically once the databases
                // answer again. Off the UI thread; outcomes go to the
                // diagnostic log.
                _ = Task.Run(() =>
                {
                    try
                    {
                        var outcome = graph.Backfill.ReplayPending(
                            line => graph.Log.Info("backfill", line));
                        if (outcome.Resolved + outcome.Unresolvable + outcome.StillPending > 0)
                        {
                            graph.Log.Info("backfill",
                                $"replay done: {outcome.Resolved} resolved, " +
                                $"{outcome.Unresolvable} unresolvable, " +
                                $"{outcome.StillPending} still pending");
                        }
                    }
                    catch (Exception ex)
                    {
                        graph.Log.Warn("backfill", "replay failed: " + ex.GetType().Name);
                    }
                });
            };
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}

public static class Program
{
    internal static readonly Stopwatch Startup = Stopwatch.StartNew();

    [STAThread]
    public static void Main(string[] args) => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .StartWithClassicDesktopLifetime(args);
}
