using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CUETools.Linux.App.Services;
using CUETools.Wpf.Services;

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

            // Window role and validated drive letter arrive only through the
            // launch arguments (the process-per-drive contract).
            var launchOptions = CUETools.Wpf.Services.AppLaunchOptions.Parse(
                desktop.Args ?? Array.Empty<string>());

            MainWindow? windowRef = null;
            Composition.AppGraph graph = Composition.CreateAppGraph(
                new AvaloniaFileDialogService(() => windowRef),
                autoRepair ? new AutoConfirmPrompt() : new AvaloniaUserPrompt(() => windowRef),
                new AvaloniaUiDispatcher(),
                launchOptions);
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
                // Secondary drive windows never publish shared settings; the
                // primary window owns the durable profile.
                if (!launchOptions.IsSecondaryDriveWindow)
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
            if (launchOptions.IsSecondaryDriveWindow &&
                launchOptions.PreferredDrive != '\0')
            {
                window.Title = $"CUETools Linux - Drive {launchOptions.PreferredDrive}";
                window.ShowRipPage();
            }
            windowRef = window;
            window.Opened += (_, _) =>
            {
                // --smoke: prove the app reaches a visible window, then exit
                // (used by CI and startup measurements; the stopwatch starts
                // in Main so the number covers the whole launch).
                long startupMs = Program.Startup.ElapsedMilliseconds;
                Console.WriteLine($"startup-to-window-ms={startupMs}");
                if (desktop.Args is ["--smoke", ..])
                {
                    // The number is asserted, not just printed. This line read 0 on
                    // every run for as long as it existed, because the stopwatch was a
                    // beforefieldinit static initializer that did not run until this
                    // read, and CI never noticed because nothing checked the value. A
                    // real launch cannot reach a visible window in under a millisecond,
                    // so 0 means the instrument is broken rather than the app is fast.
                    if (startupMs <= 0)
                    {
                        Console.Error.WriteLine(
                            "smoke: startup-to-window-ms is 0, which no real launch achieves. " +
                            "The startup stopwatch is not being started. See findings F-41.");
                        Console.Out.Flush();
                        Console.Error.Flush();
                        Environment.Exit(1);
                    }

                    // Hard exit by design: --smoke exists to prove the app
                    // reaches a visible window. Graceful lifetime shutdown
                    // from inside Opened races StartCore's own use of the
                    // window (observed NullReferenceException on the X11
                    // backend), and a diagnostics path has nothing to tear
                    // down gracefully.
                    Console.Out.Flush();
                    Environment.Exit(0);
                }

                // First run asks once whether CUETools may look up cover art on
                // its own, then remembers the answer (the same shape as the
                // database-submission consent). Defaulting it off in silence
                // would leave a feature that looks broken with no sign a choice
                // exists. Secondary drive windows never ask: the primary window
                // owns the profile, and two windows asking one question at once
                // is not a question, it is a pile-up.
                if (!launchOptions.IsSecondaryDriveWindow &&
                    !autoRepair &&
                    NetworkPreferences.NeedsArtworkAnswer(graph.Settings))
                {
                    _ = NetworkPreferences
                        .AskAboutArtworkAsync(
                            graph.Settings,
                            new AvaloniaUserPrompt(() => windowRef))
                        .ContinueWith(
                            answer => graph.Log.Info(
                                "settings",
                                "artwork auto-lookup answered: " +
                                (answer.Result ? "yes" : "no")),
                            TaskScheduler.Default);
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
                if (args.Contains("--enrich"))
                {
                    // --enrich is explicit command-line consent (the --repair
                    // precedent): the proposal's diff is applied without the
                    // interactive approval dialog, and the consent is logged.
                    string? source = paths.FirstOrDefault();
                    if (source != null)
                    {
                        graph.Log.Info("enrich",
                            "--enrich: diff auto-approved by command-line consent");
                        _ = Task.Run(() =>
                        {
                            try
                            {
                                var proposal = graph.Enrichment.Propose(source);
                                if (proposal is { HasChanges: true })
                                {
                                    int files = graph.Enrichment.Apply(proposal);
                                    graph.Log.Info("enrich",
                                        $"--enrich applied {proposal.Changes.Count} change(s) across {files} file(s) from {proposal.Provider}");
                                }
                                else
                                {
                                    graph.Log.Info("enrich",
                                        proposal == null
                                            ? "--enrich: no database release found"
                                            : "--enrich: album already matches the database");
                                }
                            }
                            catch (Exception ex)
                            {
                                graph.Log.Warn("enrich", "--enrich failed: " + ex.GetType().Name);
                            }
                        });
                    }
                }
                else if (args.Contains("--rip-page"))
                {
                    // Land on the Rip page (desktop-integration parity with
                    // --queue / --convert).
                    window.ShowRipPage();
#if RIP_DIAGNOSTIC
                    // Dev-only evidence hook (D-053): --rip-verify starts a
                    // verify once the inserted disc has been read.
                    int verifyIndex = Array.IndexOf(args, "--rip-verify");
                    if (verifyIndex >= 0)
                    {
                        var ripVm = graph.Rip;
                        char wantedDrive = verifyIndex + 1 < args.Length && args[verifyIndex + 1].Length == 1
                            ? char.ToUpperInvariant(args[verifyIndex + 1][0])
                            : '\0';
                        // Require several consecutive settled ticks so the
                        // selected drive's own disc read has fully replaced any
                        // earlier drive's state before the verify fires (the
                        // engine's disc-mismatch guard refuses mixed state).
                        int settled = 0;
                        var autoVerify = new Avalonia.Threading.DispatcherTimer
                        {
                            Interval = TimeSpan.FromSeconds(1),
                        };
                        autoVerify.Tick += (_, _) =>
                        {
                            // Startup enumeration can override an early drive
                            // selection; re-assert each tick until it holds
                            // (the setter re-reads the disc on a real change),
                            // then require the state to settle before firing.
                            if (wantedDrive != '\0' && ripVm.SelectedDrive != wantedDrive)
                            {
                                ripVm.SelectedDrive = wantedDrive;
                                settled = 0;
                                return;
                            }
                            settled = ripVm.IsDiscPresent && !ripVm.IsBusy && !ripVm.IsRipping
                                ? settled + 1
                                : 0;
                            if (settled >= 4)
                            {
                                autoVerify.Stop();
                                ripVm.VerifyCommand.Execute(null);
                            }
                        };
                        autoVerify.Start();
                    }
#endif
                }
                else if (args.Contains("--queue"))
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
                //
                // Secondary drive windows do not replay. The primary window owns
                // the durable profile and the journal, exactly as it owns settings
                // (the replay itself also claims a cross-process lock, so this is
                // the cheap half of the same rule).
                if (!launchOptions.IsSecondaryDriveWindow)
                {
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
                }
            };
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}

public static class Program
{
    // Deliberately NOT Stopwatch.StartNew() in the initializer. Program has no static
    // constructor, so the compiler marks it beforefieldinit and the runtime may defer
    // this initializer until the first access to one of these fields. That access was
    // the ElapsedMilliseconds read itself, so the stopwatch started and was read in the
    // same instant and startup-to-window-ms printed 0 on every run. Started explicitly
    // as the first statement of Main instead.
    internal static readonly Stopwatch Startup = new Stopwatch();

    [STAThread]
    public static void Main(string[] args)
    {
        Startup.Start();

#if RIP_DIAGNOSTIC
        // Dev-only (D-053): the rip transport proof runs before any UI
        // exists and exits with the failed-drive count. Compiled out of
        // Release builds, where the flag does not exist.
        if (args.Contains("--rip-diagnostic"))
        {
            Environment.Exit(Services.RipDiagnostic.Run());
        }
        // --rip-tc <letter>: the full secure Test & Copy transaction against
        // one drive into a scratch directory (increment 4 evidence).
        int verifyCliIndex = Array.IndexOf(args, "--rip-verify-cli");
        if (verifyCliIndex >= 0)
        {
            char cliLetter = verifyCliIndex + 1 < args.Length && args[verifyCliIndex + 1].Length == 1
                ? char.ToUpperInvariant(args[verifyCliIndex + 1][0])
                : 'A';
            Environment.Exit(Services.RipDiagnostic.RunVerifyCli(cliLetter));
        }
        int seqIndex = Array.IndexOf(args, "--rip-seq-probe");
        if (seqIndex >= 0)
        {
            char seqLetter = seqIndex + 1 < args.Length && args[seqIndex + 1].Length == 1
                ? char.ToUpperInvariant(args[seqIndex + 1][0])
                : 'C';
            Environment.Exit(Services.RipDiagnostic.RunSequenceProbe(seqLetter));
        }
        int probeIndex = Array.IndexOf(args, "--rip-probe");
        if (probeIndex >= 0)
        {
            char probeLetter = probeIndex + 1 < args.Length && args[probeIndex + 1].Length == 1
                ? char.ToUpperInvariant(args[probeIndex + 1][0])
                : 'A';
            Environment.Exit(Services.RipDiagnostic.RunReadShapeProbe(probeLetter));
        }
        int tcIndex = Array.IndexOf(args, "--rip-tc");
        if (tcIndex >= 0)
        {
            char letter = tcIndex + 1 < args.Length && args[tcIndex + 1].Length == 1
                ? char.ToUpperInvariant(args[tcIndex + 1][0])
                : 'A';
            Environment.Exit(Services.RipDiagnostic.RunTestCopy(letter));
        }
#endif
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime(args);
    }
}
