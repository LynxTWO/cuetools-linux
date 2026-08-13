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
    public static void RegisterManagedCodecs()
    {
        CUEProcessorPlugins.decs.Add(new CUETools.Codecs.Flake.DecoderSettings());
        CUEProcessorPlugins.encs.Add(new CUETools.Codecs.Flake.EncoderSettings());
        CUEProcessorPlugins.decs.Add(new CUETools.Codecs.ALAC.DecoderSettings());
        CUEProcessorPlugins.encs.Add(new CUETools.Codecs.ALAC.EncoderSettings());
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
        return loader;
    }

    /// <summary>
    /// Mirrors the WPF head's CreateWpfDefaultConfig (App.xaml.cs) until
    /// settings persistence lands on Linux. Kept byte-for-byte in sync by
    /// review; the values are the modern app's defaults.
    /// </summary>
    public static CUEConfig CreateDefaultConfig()
    {
        var config = new CUEConfig
        {
            detectHDCD = true,
            decodeHDCD = false,
            decodeHDCDto24bit = false,
            maxAlbumArtSize = 1500,
            writeArTagsOnEncode = true,
            CopyAlbumArt = false,
        };
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
        VerificationBackfillService Backfill,
        IDiagnosticLog Log,
        CUEConfig Config,
        EncoderCatalog Catalog);

    public static AppGraph CreateAppGraph(
        IFileDialogService dialogs, IUserPrompt prompts, IUiDispatcher dispatcher)
    {
        CUEConfig config = CreateDefaultConfig();
        IDiagnosticLog log = new DiagnosticLog();
        var journal = new JournalStore();
        IVerifyService engineVerify = new VerifyService(config, log);
        IVerifyService journaledVerify = new JournalingVerifyService(engineVerify, journal);
        var viewModel = new VerifyViewModel(
            journaledVerify,
            new ReportStore(log),
            new VerificationSourceDiscovery(config),
            dialogs,
            prompts,
            dispatcher);
        // Replay re-verifies through the raw engine service: a replay that
        // happens to race a network drop must not journal a duplicate of the
        // entry it is resolving.
        var backfill = new VerificationBackfillService(engineVerify, journal);

        // Convert stack: catalog + service + page. Settings persistence is a
        // later slice; a fresh AppSettings carries the modern defaults, same
        // stance as CreateDefaultConfig above.
        var appSettings = new AppSettings();
        var catalog = new EncoderCatalog(log, appSettings);
        IConvertService convert = new ConvertService(config, catalog, appSettings);
        var convertViewModel = new ConvertViewModel(
            convert, catalog, config, dialogs, dispatcher);

        // The queue verifies through the journaled service, so offline batch
        // verifies journal for backfill exactly like the Verify page's.
        var queueViewModel = new QueueViewModel(
            journaledVerify, convert, catalog, config, dialogs, dispatcher);

        return new AppGraph(
            viewModel, convertViewModel, queueViewModel, backfill, log, config, catalog);
    }
}
