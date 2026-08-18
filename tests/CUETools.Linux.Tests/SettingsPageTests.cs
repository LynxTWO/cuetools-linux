using CUETools.Linux.App;
using CUETools.Processor;
using CUETools.Wpf.Services;
using CUETools.Wpf.ViewModels;
using Xunit;

namespace CUETools.Linux.Tests;

// The settings page (D-073). Changes apply to the live config immediately; the file
// still writes once at exit (D-043), which SettingsPersistenceTests already covers.
public class SettingsPageTests
{
    private sealed class NullLog : IDiagnosticLog
    {
        public string LogPath => "/tmp/nowhere.log";
        public void Info(string category, string message) { }
        public void Warn(string category, string message) { }
        public void Error(string category, string message, Exception? ex = null) { }
        public void Redact(params string?[] sensitive) { }
    }

    private static (SettingsViewModel vm, CUEConfig config, AppSettings app) Create()
    {
        CUEConfig config = Composition.CreateDefaultConfig();
        var app = new AppSettings();
        var vm = new SettingsViewModel(
            config, app, new NullLog(), new EncoderCatalog(new NullLog(), app));
        return (vm, config, app);
    }

    [Fact]
    public void SharingChoiceMapsOntoTheTwoEngineKeys()
    {
        var (vm, config, _) = Create();

        vm.CtdbSharingChoice = "Always share";
        Assert.False(config.advanced.CTDBAsk);
        Assert.True(config.advanced.CTDBSubmit);
        Assert.True(CtdbSubmissionPolicy.IsEnabled(config));

        vm.CtdbSharingChoice = "Never share";
        Assert.False(config.advanced.CTDBAsk);
        Assert.False(config.advanced.CTDBSubmit);
        Assert.False(CtdbSubmissionPolicy.IsEnabled(config));

        // Back to asking re-arms the dialog without inventing an answer.
        vm.CtdbSharingChoice = "Ask before sharing";
        Assert.True(config.advanced.CTDBAsk);
        Assert.Equal("Ask before sharing", vm.CtdbSharingChoice);
    }

    [Fact]
    public void SharingChoiceReadsWhateverTheDialogRemembered()
    {
        var (vm, config, _) = Create();

        // The consent dialog's remember checkbox writes these same keys, so the page
        // must read them back as the words the user would recognise.
        CtdbSubmissionEligibility.Remember(config, submit: true);
        Assert.Equal("Always share", vm.CtdbSharingChoice);

        CtdbSubmissionEligibility.Remember(config, submit: false);
        Assert.Equal("Never share", vm.CtdbSharingChoice);
    }

    [Fact]
    public void AlwaysShareStillCannotPushAnUncleanRip()
    {
        var (vm, config, _) = Create();
        vm.CtdbSharingChoice = "Always share";

        // D-070 outranks the remembered yes. The settings page must not create a path
        // around the quality gate.
        CtdbSubmissionDecision decision = CtdbSubmissionEligibility.Decide(
            new CtdbSubmissionCandidate { RunCompleted = true, Salvaged = true }, config);

        Assert.Equal(CtdbSubmissionBlock.Salvaged, decision.Block);
    }

    [Fact]
    public void TouchingTheArtworkToggleCountsAsAnswering()
    {
        var (vm, _, app) = Create();
        Assert.True(NetworkPreferences.NeedsArtworkAnswer(app));

        vm.ArtworkAutoLookup = true;

        Assert.True(app.AutoFetchArtOnDiscRead);
        // The first-run dialog must not re-ask a question the user answered here.
        Assert.False(NetworkPreferences.NeedsArtworkAnswer(app));
    }

    [Fact]
    public void KeepLogsForeverReachesTheRunningLogger()
    {
        var (vm, _, app) = Create();
        bool before = DiagnosticLog.KeepLogsForever;
        try
        {
            vm.KeepLogsForever = true;
            Assert.True(app.KeepLogsForever);
            // Mirrored to the logger's own gate so the choice needs no restart.
            Assert.True(DiagnosticLog.KeepLogsForever);

            vm.KeepLogsForever = false;
            Assert.False(DiagnosticLog.KeepLogsForever);
        }
        finally
        {
            DiagnosticLog.KeepLogsForever = before;
        }
    }

    [Fact]
    public void ArtSizeClampsToTheRangeTheLoaderEnforces()
    {
        var (vm, config, _) = Create();
        // Out-of-range values used to survive the session, hit settings.txt, and be
        // rejected by the loader on the next launch, silently reverting the page.
        vm.MaxAlbumArtSize = 12000;
        Assert.Equal(10000, config.maxAlbumArtSize);
        vm.MaxAlbumArtSize = 5;
        Assert.Equal(100, config.maxAlbumArtSize);
    }

    [Fact]
    public void EngineTogglesWriteStraightToTheLiveConfig()
    {
        var (vm, config, _) = Create();
        vm.TrackFilenameFormat = "%tracknumber% - %title%";
        Assert.Equal("%tracknumber% - %title%", config.trackFilenameFormat);

        bool eject = config.ejectAfterRip;
        vm.EjectAfterRip = !eject;
        Assert.Equal(!eject, config.ejectAfterRip);
    }
}
