using CUETools.Linux.App.Journal;
using CUETools.Linux.App.Services;
using CUETools.Processor;
using CUETools.Wpf.Services;
using CUETools.Wpf.ViewModels;

namespace CUETools.Linux.App;

/// <summary>
/// Hand-wired composition for the verify slice. A DI container arrives when
/// the page count justifies one; explicit construction keeps the object
/// graph visible and NativeAOT-friendly.
/// </summary>
public static class Composition
{
    /// <summary>
    /// Registers the compiled-in managed codecs with the engine's public
    /// registries. Linux v1 policy: codecs are compiled in and registered
    /// explicitly; there is no runtime plugin scanning (AOT-compatible, and
    /// a deliberately smaller trust surface than the Windows head's
    /// manifest-gated plugin directory). WAV is registered by the engine
    /// itself; Flake adds FLAC, and ALAC adds m4a.
    /// </summary>
    /// <remarks>
    /// The engine's plugin registries are static Lists and CUEConfig's
    /// constructor iterates them, so every registration and every config
    /// construction serializes on one lock. The app registers once at
    /// startup; the parallel test host is where writer-vs-reader races were
    /// real (observed once in CI: "Collection was modified" inside
    /// CUEToolsCodecsConfig.Init).
    /// </remarks>
    private static readonly object CodecRegistryLock = new();
    private static bool _managedCodecsRegistered;

    public static void RegisterManagedCodecs()
    {
        lock (CodecRegistryLock)
        {
            if (_managedCodecsRegistered)
                return;
            _managedCodecsRegistered = true;
            CUEProcessorPlugins.decs.Add(new CUETools.Codecs.Flake.DecoderSettings());
            CUEProcessorPlugins.encs.Add(new CUETools.Codecs.Flake.EncoderSettings());
            CUEProcessorPlugins.decs.Add(new CUETools.Codecs.ALAC.DecoderSettings());
            CUEProcessorPlugins.encs.Add(new CUETools.Codecs.ALAC.EncoderSettings());
        }
    }

    /// <summary>
    /// Validates and loads the vendored native codec libraries (D-042), then
    /// registers each codec whose library is ready. Codec readiness rule: a
    /// codec whose .so failed validation is simply not registered - never a
    /// selectable lie - and the reason is logged per library.
    /// </summary>
    public static NativeCodecLoader RegisterNativeCodecs(Action<string> log)
    {
        var loader = new NativeCodecLoader();
        loader.LoadAll(log);

        lock (CodecRegistryLock)
        {
        if (loader.IsReady("libFLAC_dynamic"))
        {
            loader.BindResolver(typeof(CUETools.Codecs.libFLAC.DecoderSettings).Assembly);
            CUEProcessorPlugins.decs.Add(new CUETools.Codecs.libFLAC.DecoderSettings());
            CUEProcessorPlugins.encs.Add(new CUETools.Codecs.libFLAC.EncoderSettings());
        }
        if (loader.IsReady("wavpackdll"))
        {
            loader.BindResolver(typeof(CUETools.Codecs.libwavpack.DecoderSettings).Assembly);
            CUEProcessorPlugins.decs.Add(new CUETools.Codecs.libwavpack.DecoderSettings());
            CUEProcessorPlugins.encs.Add(new CUETools.Codecs.libwavpack.EncoderSettings());
        }
        if (loader.IsReady("MACLibDll"))
        {
            loader.BindResolver(typeof(CUETools.Codecs.MACLib.DecoderSettings).Assembly);
            CUEProcessorPlugins.decs.Add(new CUETools.Codecs.MACLib.DecoderSettings());
            CUEProcessorPlugins.encs.Add(new CUETools.Codecs.MACLib.EncoderSettings());
        }
        if (loader.IsReady("libmp3lame"))
        {
            // Encoder-only: MP3 output (CBR and VBR faces); decode is not part
            // of the lame wrapper.
            loader.BindResolver(typeof(CUETools.Codecs.libmp3lame.LameEncoderSettings).Assembly);
            CUEProcessorPlugins.encs.Add(new CUETools.Codecs.libmp3lame.CBREncoderSettings());
            CUEProcessorPlugins.encs.Add(new CUETools.Codecs.libmp3lame.VBREncoderSettings());
        }
        }
        return loader;
    }

    /// <summary>
    /// Constructs a bare CUEConfig under the registry lock. Every config
    /// construction reads the static plugin lists, so it must serialize
    /// against registration; tests that want an unconfigured engine config
    /// use this instead of calling the constructor directly.
    /// </summary>
    internal static CUEConfig CreateEngineConfig()
    {
        lock (CodecRegistryLock)
        {
            return new CUEConfig();
        }
    }

    /// <summary>
    /// Mirrors the WPF head's CreateWpfDefaultConfig (App.xaml.cs) until
    /// settings persistence lands on Linux. Kept byte-for-byte in sync by
    /// review; the values are the modern app's defaults.
    /// </summary>
    public static CUEConfig CreateDefaultConfig()
    {
        CUEConfig config = CreateEngineConfig();
        config.detectHDCD = true;
        config.decodeHDCD = false;
        config.decodeHDCDto24bit = false;
        config.maxAlbumArtSize = 1500;
        config.writeArTagsOnEncode = true;
        config.CopyAlbumArt = false;
        config.advanced.CreateTOC = true;
        config.advanced.DetailedCTDBLog = true;
        config.advanced.coversSearch = CUEConfigAdvanced.CTDBCoversSearch.Extensive;
        // Linux v1 has no settings page yet, and R-001's acceptance test
        // requires a dated report file next to the verified album, so the
        // engine's own AccurateRip log writer is on by default here (the
        // WPF head leaves it to its settings page).
        config.writeArLogOnVerify = true;
        return config;
    }

    public sealed record AppGraph(
        VerifyViewModel Verify,
        ConvertViewModel Convert,
        QueueViewModel Queue,
        SettingsViewModel SettingsPage,
        ReportViewModel Report,
        NamingViewModel Naming,
        VerificationBackfillService Backfill,
        IDiagnosticLog Log,
        CUEConfig Config,
        EncoderCatalog Catalog,
        AppSettings Settings,
        SettingsStore SettingsStore,
        IEnrichmentService Enrichment,
        JournalStore Journal,
        RipViewModel Rip,
        IAlbumArtService Art);

    private static IReadOnlyDictionary<string, string> LoadPackagedEncoderHashes(
        IDiagnosticLog log)
    {
        var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string manifestPath = Path.Combine(
            AppContext.BaseDirectory, "encoders", "linux-encoders.json");
        try
        {
            if (File.Exists(manifestPath))
            {
                var manifest = System.Text.Json.JsonSerializer.Deserialize(
                    File.ReadAllBytes(manifestPath),
                    Services.LinuxEncoderManifestJsonContext.Default.LinuxEncoderManifest);
                foreach (Services.LinuxEncoderEntry entry in manifest?.Encoders ?? new())
                {
                    if (!string.IsNullOrWhiteSpace(entry.File) &&
                        !string.IsNullOrWhiteSpace(entry.Sha256))
                    {
                        hashes[entry.File!] = entry.Sha256!;
                    }
                }
                log.Info("encoders", $"packaged encoder table: {hashes.Count} entries");
            }
            else
            {
                log.Info("encoders", "no packaged encoder manifest; curated command encoders unavailable");
            }
        }
        catch (Exception ex)
        {
            log.Warn("encoders", "packaged encoder manifest unreadable: " + ex.GetType().Name);
            hashes.Clear();
        }
        return hashes;
    }

    /// <param name="submissionPrompt">
    /// SLICE-012 consent surface. Null, the default, means no submission is ever offered:
    /// the service treats a missing prompt as a refusal. Tests and every caller that does
    /// not pass one therefore cannot upload anything, which is the behaviour that shipped
    /// before the dialog existed.
    /// </param>
    public static AppGraph CreateAppGraph(
        IFileDialogService dialogs, IUserPrompt prompts, IUiDispatcher dispatcher,
        AppLaunchOptions? launchOptions = null,
        ICtdbSubmissionPrompt? submissionPrompt = null)
    {
        launchOptions ??= new AppLaunchOptions();
        CUEConfig config = CreateDefaultConfig();
        IDiagnosticLog log = new DiagnosticLog();

        // Settings persistence (SLICE-006, D-043): the shared SettingsStore
        // reads the same profile format the WPF head writes, at the
        // ApplicationData CUETools2026 location (~/.config/CUETools2026 on
        // Linux). Loading BEFORE the catalog and services are built means
        // saved encoder choices, naming, and output settings shape the
        // session; the shell saves on exit. Secrets are never persisted on
        // this head (DecliningSecretProtector).
        var appSettings = new AppSettings();
        var settingsStore = new SettingsStore(
            log, null, new DecliningSecretProtector());
        settingsStore.Load(config, appSettings);

        var journal = new JournalStore();
        // One report store for the whole graph. Verify and Rip each built their own
        // before the Report page existed; with a page listening for Changed, split
        // stores would show only one lane's receipts.
        var reports = new ReportStore(log);
        // One service for the whole session, so a remembered answer applies everywhere.
        // Null prompt means it can only ever refuse, which is what keeps every non-GUI
        // caller unable to upload.
        var submissions = submissionPrompt == null
            ? null
            : new CtdbSubmissionService(config, submissionPrompt, log);
        var engineVerifyService = new VerifyService(config, log) { Submissions = submissions };
        IVerifyService engineVerify = engineVerifyService;
        // Probe the databases the way the lookups will actually reach them. Without the
        // proxy, a network that allows only proxied outbound reported offline for services
        // the app could reach, journaling every verify and failing every replay the same way.
        bool ProbeOnline() => ConnectivityProbe.IsOnline(config.GetProxy());
        IVerifyService journaledVerify =
            new JournalingVerifyService(engineVerify, journal, ProbeOnline);
        var viewModel = new VerifyViewModel(
            journaledVerify,
            reports,
            new VerificationSourceDiscovery(config),
            dialogs,
            prompts,
            dispatcher);
        // Replay re-verifies through the raw engine service: a replay that
        // happens to race a network drop must not journal a duplicate of the
        // entry it is resolving.
        var backfill = new VerificationBackfillService(engineVerify, journal, ProbeOnline);

        // Convert stack: catalog + service + page, built on the LOADED
        // settings so saved codec selections and format types apply. The
        // catalog gets this platform's packaged-encoder trust table (the
        // hash manifest the pinned-source CLI encoder build generated,
        // D-047); a missing manifest means an empty table and the curated
        // command encoders simply stay unavailable with the reason shown.
        var catalog = new EncoderCatalog(
            log,
            appSettings,
            Path.Combine(AppContext.BaseDirectory, "encoders"),
            LoadPackagedEncoderHashes(log));
        catalog.EnsureRegistered(config);
        IConvertService convert = new ConvertService(config, catalog, appSettings);
        var convertViewModel = new ConvertViewModel(
            convert, catalog, config, dialogs, dispatcher);

        // The queue verifies through the journaled service, so offline batch
        // verifies journal for backfill exactly like the Verify page's.
        var queueViewModel = new QueueViewModel(
            journaledVerify, convert, catalog, config, dialogs, dispatcher);

        IEnrichmentService enrichment = new EnrichmentService(config, log, journal);

        // Rip stack (SLICE-009 increment 5): the same services the WPF head
        // composes, over the SG_IO transport. Calibration and verify history
        // persist under the app-data profile like everything else.
        var calStore = new CUETools.Wpf.Accuracy.DriveCalibrationStore();
        var verifyHistory = new CUETools.Wpf.Accuracy.VerifyHistoryStore();
        var calService = new CUETools.Wpf.Accuracy.DriveCalibrationService(log, calStore);
        IDriveService drives = new DriveService(config, log);
        // Publish a requested drive before the rip view model is constructed
        // (the WPF secondary-window pattern): pages then initialize against
        // that hardware with no transient read of the first enumerated drive.
        if (launchOptions.PreferredDrive != '\0')
        {
            if (drives.GetDrives().Contains(launchOptions.PreferredDrive))
                drives.SelectedDrive = launchOptions.PreferredDrive;
            else
                log.Warn("app",
                    $"requested drive {launchOptions.PreferredDrive}: is not attached");
        }
        IRipService ripService = new RipService(
            config, log, appSettings, catalog, calStore, verifyHistory, calService)
        {
            Submissions = submissions,
        };
        // SLICE-011: the guided-recovery affordance. The probe is the Linux sysfs +
        // ioctl implementation; a platform without one would leave Recovery unset and
        // the offer never shows.
        var recoveryServices = new CUETools.Wpf.Accuracy.DriveRecoveryServices(
            new LinuxDriveRecoveryProbe(log),
            new CUETools.Wpf.Accuracy.DriveRecoveryIncidentStore());
        IHistoryStore ripHistory = new HistoryStore(log);
        IAlbumArtService albumArt = new AlbumArtService(
            config, appSettings, log, new SkiaImageTranscoder());
        var ripViewModel = new RipViewModel(
            drives,
            ripService,
            engineVerify,
            convert,
            reports,
            ripHistory,
            config,
            albumArt,
            appSettings,
            catalog,
            new AppStatusService(),
            settingsStore,
            launchOptions,
            log,
            new AvaloniaUiTimerFactory(),
            dispatcher,
            prompts,
            new AvaloniaArtworkPreviewFactory(),
            new LinuxPlatformCapabilities(),
            dialogs);
        ripViewModel.Recovery = recoveryServices;
        // The tray-disc preview pulls the Rip page lazily through the seam.
        var namingPage = new NamingViewModel(config, appSettings, () => ripViewModel);

        // A rip makes its own database contact rather than going through the journaled verify
        // service, so an offline rip used to finish with no verdict and nothing queued: the
        // user had to know to re-verify the finished album by hand. Queue it instead, so the
        // backfill asks on a later launch exactly as it does for an offline verify.
        ripViewModel.Published += publication =>
        {
            if (!publication.NoDatabaseAnswered) return;
            try
            {
                string? source = RipPublicationSource(publication.OutputDirectory);
                if (source == null)
                {
                    log.Warn("backfill", "published rip has no manifest to queue");
                    return;
                }
                journal.CreatePending(BackfillLane.Verification, source, "");
                log.Info("backfill", "offline rip queued for automatic verification");
            }
            catch (Exception ex)
            {
                log.Warn("backfill", "could not queue the published rip: " + ex.GetType().Name);
            }
        };

        // D-073: bound to the live config, applies immediately, saved once at exit by
        // the existing save-on-exit contract. The dialog seam powers encoder Locate.
        var settingsPage = new SettingsViewModel(config, appSettings, log, catalog, dialogs);
        var reportPage = new ReportViewModel(reports);

        return new AppGraph(
            viewModel, convertViewModel, queueViewModel, settingsPage, reportPage, namingPage, backfill, log, config,
            catalog, appSettings, settingsStore, enrichment, journal, ripViewModel,
            albumArt);
    }

    /// <summary>
    /// The manifest inside a published rip that a later verify should re-check: its CUE sheet,
    /// or the single audio file an image-layout rip wrote. Journaling the directory itself
    /// would queue a path the engine cannot open ("is a directory").
    /// </summary>
    private static string? RipPublicationSource(string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory) ||
            !System.IO.Directory.Exists(outputDirectory))
        {
            return null;
        }

        string[] cues = System.IO.Directory.GetFiles(outputDirectory, "*.cue");
        if (cues.Length == 1) return cues[0];
        if (cues.Length > 1)
        {
            // A rip publishes one cue per album folder. More than one means something else
            // put a sheet here, and guessing which describes the rip is exactly the kind of
            // choice discovery refuses to make.
            return null;
        }

        foreach (string extension in new[] { "*.flac", "*.wv", "*.ape", "*.m4a", "*.wav" })
        {
            string[] audio = System.IO.Directory.GetFiles(outputDirectory, extension);
            if (audio.Length == 1) return audio[0];
        }
        return null;
    }
}
