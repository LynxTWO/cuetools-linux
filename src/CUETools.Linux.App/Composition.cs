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
        return config;
    }

    public static VerifyViewModel CreateVerifyViewModel(
        IFileDialogService dialogs, IUserPrompt prompts, IUiDispatcher dispatcher)
    {
        CUEConfig config = CreateDefaultConfig();
        IDiagnosticLog log = new DiagnosticLog();
        return new VerifyViewModel(
            new VerifyService(config, log),
            new ReportStore(log),
            new VerificationSourceDiscovery(config),
            dialogs,
            prompts,
            dispatcher);
    }
}
