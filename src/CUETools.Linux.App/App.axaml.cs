using System.Diagnostics;
using System.Runtime.InteropServices;
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
            // Native codec registration is logged through the app graph's
            // diagnostic log once it exists; buffer the lines until then.
            var nativeLog = new List<string>();
            Composition.RegisterNativeCodecs(nativeLog.Add);
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
            foreach (string line in nativeLog) graph.Log.Info("codecs", line);

            // A Linux session manager stops apps with SIGTERM (and a terminal
            // with SIGINT); route both through the graceful lifetime shutdown
            // so save-on-exit runs instead of the process dying mid-state.
            PosixSignalRegistration? sigterm = null, sigint = null;
            void RequestShutdown(PosixSignalContext context)
            {
                context.Cancel = true;
                Avalonia.Threading.Dispatcher.UIThread.Post(() => desktop.TryShutdown());
            }
            sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, RequestShutdown);
            sigint = PosixSignalRegistration.Create(PosixSignal.SIGINT, RequestShutdown);

            // Settings persist on exit, mirroring the WPF head's
            // load-once/save-on-exit contract (SLICE-006, D-043). Exit fires
            // on every graceful path (window close, TryShutdown, Shutdown);
            // ShutdownRequested does not cover the forced one.
            desktop.Exit += (_, _) =>
            {
                graph.SettingsStore.Save(graph.Config, graph.Settings);
                sigterm?.Dispose();
                sigint?.Dispose();
            };

            var verify = graph.Verify;
            if (autoRepair)
            {
                graph.Log.Info("repair", "--repair: confirmations auto-accepted by command-line consent");
                _ = new AutoRepairDriver(verify, line => graph.Log.Info("repair", line));
            }

            var window = new MainWindow(theme, verify, graph.Convert, graph);
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
                // --convert routes the path to the Convert page instead and
                // starts the conversion (--convert-out <dir> sets the output
                // folder; default is the page's usual Music/CUETools layout).
                string[] args = desktop.Args ?? Array.Empty<string>();
                string[] paths = args
                    .Where(arg => File.Exists(arg) || Directory.Exists(arg))
                    .ToArray();
                if (args.Contains("--queue"))
                {
                    // --queue enqueues every path argument under the current
                    // action defaults and lands on the Queue page; --queue-run
                    // additionally starts the batch.
                    window.ShowQueuePage();
                    int queued = paths.Count(p => graph.Queue.EnqueuePath(p));
                    if (queued > 0 && args.Contains("--queue-run"))
                    {
                        graph.Queue.RunAllCommand.Execute(null);
                    }
                }
                else if (args.Contains("--convert"))
                {
                    int outIndex = Array.IndexOf(args, "--convert-out");
                    string? outDir = outIndex >= 0 && outIndex + 1 < args.Length
                        ? args[outIndex + 1]
                        : null;
                    int toIndex = Array.IndexOf(args, "--convert-to");
                    string? format = toIndex >= 0 && toIndex + 1 < args.Length
                        ? args[toIndex + 1]
                        : null;
                    if (format != null && graph.Convert.Formats.Contains(format))
                    {
                        graph.Convert.SelectedFormat = format;
                    }
                    // --convert always lands on the Convert page; a valid
                    // source additionally starts the conversion.
                    window.ShowConvertPage();
                    string? source = paths.FirstOrDefault(p => p != outDir);
                    if (source != null && graph.Convert.LoadSource(source, outDir))
                    {
                        graph.Convert.ConvertCommand.Execute(null);
                    }
                }
                else if (paths.Length > 0 && verify.LoadSources(paths) &&
                    (args.Contains("--verify") || autoRepair))
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
