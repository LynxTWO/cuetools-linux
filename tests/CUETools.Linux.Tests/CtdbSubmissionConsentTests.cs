using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using CUETools.CDImage;
using CUETools.CTDB;
using CUETools.Linux.App;
using CUETools.Linux.App.Services;
using CUETools.Processor;
using CUETools.Wpf.Services;
using Xunit;

namespace CUETools.Linux.Tests;

// SLICE-012 steps 1 and 2. These are the safety properties: a submission publishes a
// user's disc identity to a public database and cannot be undone, so what is pinned here
// is mostly what must NOT happen. The eligibility rules themselves are covered by
// CtdbSubmissionEligibilityTests in the fork, which cannot run on Linux.
public class CtdbSubmissionConsentTests
{
    private sealed class RecordingPrompt : ICtdbSubmissionPrompt
    {
        public int Asked;
        public CtdbSubmissionCandidate? LastCandidate;
        public CtdbSubmissionConsent Answer = new() { Submit = false, Remember = false };

        public CtdbSubmissionConsent Ask(CtdbSubmissionCandidate candidate)
        {
            Asked++;
            LastCandidate = candidate;
            return Answer;
        }
    }

    private sealed class NullLog : IDiagnosticLog
    {
        public string LogPath => "";
        public void Info(string category, string message) { }
        public void Warn(string category, string message) { }
        public void Error(string category, string message, Exception? ex = null) { }
        public void Redact(params string?[] sensitive) { }
    }

    private static CUEToolsDB Database() => new(new CDImageLayout("0 400"), null!);

    private static CtdbSubmissionCandidate Clean() => new()
    {
        RunCompleted = true,
        LookupFailed = false,
        FailedWindows = 0,
        Salvaged = false,
        Held = false,
        Album = "Test Album",
        Artist = "Test Artist",
        Barcode = "0123456789",
        Confidence = 4,
    };

    private static CUEConfig AskingConfig()
    {
        CUEConfig config = Composition.CreateDefaultConfig();
        config.advanced.CTDBAsk = true;
        return config;
    }

    [Theory]
    // Every one of these is a disc that must never reach the dialog. Asking about a rip
    // that cannot be submitted would invite consent for an upload that then does not
    // happen, and the user would have no way to know which.
    [InlineData("salvaged")]
    [InlineData("held")]
    [InlineData("unrecoverable window")]
    [InlineData("lookup failed")]
    [InlineData("run incomplete")]
    public void AnIneligibleDiscIsNeverEvenAskedAbout(string reason)
    {
        var prompt = new RecordingPrompt { Answer = new CtdbSubmissionConsent { Submit = true } };
        CUEConfig config = AskingConfig();
        var service = new CtdbSubmissionService(config, prompt, new NullLog());

        CtdbSubmissionCandidate candidate = reason switch
        {
            "salvaged" => new CtdbSubmissionCandidate { RunCompleted = true, Salvaged = true },
            "held" => new CtdbSubmissionCandidate { RunCompleted = true, Held = true },
            "unrecoverable window" => new CtdbSubmissionCandidate { RunCompleted = true, FailedWindows = 1 },
            "lookup failed" => new CtdbSubmissionCandidate { RunCompleted = true, LookupFailed = true },
            _ => new CtdbSubmissionCandidate { RunCompleted = false },
        };

        CtdbSubmissionOutcome outcome = service.Offer(Database(), candidate);

        Assert.NotEqual(CtdbSubmissionBlock.None, outcome.Block);
        Assert.False(outcome.Submitted);
        // Nothing to say to the user about a disc that was never a candidate.
        Assert.Equal("", outcome.StatusText);
        Assert.Equal(0, prompt.Asked);
        // An unanswered question leaves the setting armed for a disc that does qualify.
        Assert.True(config.advanced.CTDBAsk);
    }

    [Fact]
    public void AHeadWithNoConsentSurfaceCannotSubmit()
    {
        CUEConfig config = AskingConfig();
        // Null prompt is the shipped state for every head that has not opted in, and the
        // reason nothing could upload before this slice.
        var service = new CtdbSubmissionService(config, null, new NullLog());

        CtdbSubmissionOutcome outcome = service.Offer(Database(), Clean());
        Assert.Equal(CtdbSubmissionBlock.DeclinedPreviously, outcome.Block);
        Assert.False(outcome.Submitted);
        Assert.True(config.advanced.CTDBAsk);
    }

    [Fact]
    public void DecliningWithoutRememberLeavesTheQuestionArmed()
    {
        var prompt = new RecordingPrompt
        {
            Answer = new CtdbSubmissionConsent { Submit = false, Remember = false },
        };
        CUEConfig config = AskingConfig();
        var service = new CtdbSubmissionService(config, prompt, new NullLog());

        CtdbSubmissionOutcome outcome = service.Offer(Database(), Clean());
        Assert.Equal(CtdbSubmissionBlock.DeclinedPreviously, outcome.Block);
        Assert.False(outcome.Submitted);
        // Declining says nothing on screen; the user already knows what they chose.
        Assert.Equal("", outcome.StatusText);
        Assert.Equal(1, prompt.Asked);
        // Not remembered, so the next disc asks again rather than inheriting this answer.
        Assert.True(config.advanced.CTDBAsk);
    }

    [Fact]
    public void DecliningWithRememberStopsTheAskingAndStaysNo()
    {
        var prompt = new RecordingPrompt
        {
            Answer = new CtdbSubmissionConsent { Submit = false, Remember = true },
        };
        CUEConfig config = AskingConfig();
        var service = new CtdbSubmissionService(config, prompt, new NullLog());

        service.Offer(Database(), Clean());
        Assert.False(config.advanced.CTDBAsk);
        Assert.False(config.advanced.CTDBSubmit);

        // A second disc must be refused from the stored answer without asking again.
        service.Offer(Database(), Clean());
        Assert.Equal(1, prompt.Asked);
    }

    [Fact]
    public void ConsentingWithRememberRecordsYesAndStopsAsking()
    {
        var prompt = new RecordingPrompt
        {
            Answer = new CtdbSubmissionConsent { Submit = true, Remember = true },
        };
        CUEConfig config = AskingConfig();
        var service = new CtdbSubmissionService(config, prompt, new NullLog());

        service.Offer(Database(), Clean());

        Assert.Equal(1, prompt.Asked);
        Assert.False(config.advanced.CTDBAsk);
        Assert.True(config.advanced.CTDBSubmit);
    }

    [Fact]
    public void AQualityBlockOutranksARememberedYes()
    {
        var prompt = new RecordingPrompt();
        CUEConfig config = Composition.CreateDefaultConfig();
        // The user once said "always submit". A salvaged capture still must not go.
        config.advanced.CTDBAsk = false;
        config.advanced.CTDBSubmit = true;
        var service = new CtdbSubmissionService(config, prompt, new NullLog());

        Assert.Equal(
            CtdbSubmissionBlock.Salvaged,
            service.Offer(Database(), new CtdbSubmissionCandidate
            {
                RunCompleted = true,
                Salvaged = true,
            }).Block);
        Assert.Equal(0, prompt.Asked);
    }

    [Fact]
    public void ThePromptIsToldWhatWouldBeUploaded()
    {
        var prompt = new RecordingPrompt();
        var service = new CtdbSubmissionService(AskingConfig(), prompt, new NullLog());

        service.Offer(Database(), Clean());

        // The dialog names what leaves the machine, so it has to be handed the values
        // rather than an album string it would have to describe from memory.
        Assert.NotNull(prompt.LastCandidate);
        Assert.Equal("Test Album", prompt.LastCandidate!.Album);
        Assert.Equal("Test Artist", prompt.LastCandidate.Artist);
        Assert.Equal("0123456789", prompt.LastCandidate.Barcode);
    }

    [Fact]
    public void ASubmissionFailureNeverChangesTheVerdict()
    {
        var prompt = new RecordingPrompt
        {
            Answer = new CtdbSubmissionConsent { Submit = true, Remember = false },
        };
        var service = new CtdbSubmissionService(AskingConfig(), prompt, new NullLog());

        // The database here has never queried anything and there is no network in a test
        // run, so the upload throws inside Offer. It must be swallowed: a verify or rip
        // verdict cannot change because a database was unreachable.
        Exception? thrown = Record.Exception(() => service.Offer(Database(), Clean()));

        Assert.Null(thrown);
    }

    [Fact]
    public void AFailedSubmissionSaysSoInsteadOfLookingLikeSuccess()
    {
        var prompt = new RecordingPrompt
        {
            Answer = new CtdbSubmissionConsent { Submit = true, Remember = false },
        };
        var service = new CtdbSubmissionService(AskingConfig(), prompt, new NullLog());

        // This database was constructed, never queried, so CUEToolsDB.Submit returns null
        // without touching the network. That is the engine refusing to send, and it is
        // also why these tests can never upload anything by accident.
        CtdbSubmissionOutcome outcome = service.Offer(Database(), Clean());

        // Silence must not extend to the user. Reported as "hit Share, then nothing": the
        // consenting user and the refusing user saw the same nothing, and so did the user
        // whose submission was never sent at all.
        Assert.False(outcome.Submitted);
        Assert.NotEqual("", outcome.StatusText);
        Assert.Contains("Could not share", outcome.StatusText);
        Assert.Contains("never looked up", outcome.StatusText);
    }
}

// The dialog itself. BuildDialog is pure layout and shows nothing, so these run headless
// and read the tree the user would see.
public class CtdbConsentDialogTests
{
    private static CtdbSubmissionCandidate Candidate(string barcode = "0123456789") => new()
    {
        RunCompleted = true,
        Album = "Aja",
        Artist = "Steely Dan",
        Barcode = barcode,
        Confidence = 4,
    };

    private static string AllText(Avalonia.Controls.Window w) =>
        string.Join("\n", w.GetLogicalDescendants()
            .OfType<Avalonia.Controls.TextBlock>()
            .Select(t => t.Text ?? ""));

    [AvaloniaFact]
    public void DecliningIsTheDefaultAndTheCancelAction()
    {
        Avalonia.Controls.Window dialog = AvaloniaCtdbSubmissionPrompt.BuildDialog(
            Candidate(), out Avalonia.Controls.CheckBox remember,
            out Avalonia.Controls.Button share, out Avalonia.Controls.Button decline);

        // Enter and Escape must both refuse. An upload cannot be undone, so a reflex
        // keypress on a dialog that appeared under the user's hands must not publish.
        Assert.True(decline.IsDefault);
        Assert.True(decline.IsCancel);
        Assert.False(share.IsDefault);
        Assert.False(share.IsCancel);

        // Remembering is opt-in: a single answer must not silently become permanent.
        Assert.False(remember.IsChecked);
        Assert.Equal("Share this rip with the CUETools Database?", dialog.Title);
    }

    [AvaloniaFact]
    public void TheDialogNamesEverythingThatLeavesTheMachine()
    {
        string text = AllText(AvaloniaCtdbSubmissionPrompt.BuildDialog(
            Candidate(), out _, out _, out _));

        // SLICE-012 section 4: the consent text must name each of these, because a user
        // cannot consent to an upload whose contents they have to guess.
        Assert.Contains("table of contents", text);
        Assert.Contains("checksum for each track", text);
        Assert.Contains("parity data", text);
        Assert.Contains("artist and album", text);
        Assert.Contains("0123456789", text);
        Assert.Contains("identifier for this computer", text);

        // And what it does not send, plus the fact that it cannot be taken back.
        Assert.Contains("does not send your audio files", text);
        Assert.Contains("cannot be", text);
        Assert.Contains("no delete", text);

        // The disc being published is named, so "this rip" is never ambiguous.
        Assert.Contains("Steely Dan - Aja", text);
    }

    [AvaloniaFact]
    public void AMissingBarcodeIsDescribedRatherThanShownAsBlank()
    {
        string text = AllText(AvaloniaCtdbSubmissionPrompt.BuildDialog(
            Candidate(barcode: ""), out _, out _, out _));

        // Never print "the disc's barcode ()" at a user.
        Assert.DoesNotContain("barcode ()", text);
        Assert.Contains("barcode, if it has one", text);
    }
}
