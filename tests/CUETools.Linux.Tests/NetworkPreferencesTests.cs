using CUETools.Wpf.Services;
using Xunit;

namespace CUETools.Linux.Tests;

// Anything the app would otherwise do on its own that reaches the network is asked once and
// remembered, rather than silently defaulted either way. Owner's call, 2026-08-16.
public class NetworkPreferencesTests
{
    private sealed class ScriptedPrompt : IUserPrompt
    {
        public ScriptedPrompt(bool answer) => _answer = answer;

        private readonly bool _answer;
        public int Asked { get; private set; }
        public string LastMessage { get; private set; } = "";
        public string LastTitle { get; private set; } = "";

        public Task<bool> ConfirmOkCancelAsync(string message, string title)
        {
            Asked++;
            LastMessage = message;
            LastTitle = title;
            return Task.FromResult(_answer);
        }
    }

    [Fact]
    public async Task AFreshProfileIsAskedOnceAndTheYesIsRemembered()
    {
        var settings = new AppSettings();
        var prompt = new ScriptedPrompt(answer: true);

        Assert.True(NetworkPreferences.NeedsArtworkAnswer(settings));

        Assert.True(await NetworkPreferences.AskAboutArtworkAsync(settings, prompt));

        Assert.Equal(1, prompt.Asked);
        Assert.True(settings.AutoFetchArtOnDiscRead);
        Assert.True(settings.AutoFetchArtAnswered);
        Assert.False(NetworkPreferences.NeedsArtworkAnswer(settings));
    }

    [Fact]
    public async Task ANoIsRememberedJustAsFirmlyAsAYes()
    {
        var settings = new AppSettings();
        var prompt = new ScriptedPrompt(answer: false);

        Assert.False(await NetworkPreferences.AskAboutArtworkAsync(settings, prompt));

        Assert.True(settings.AutoFetchArtAnswered);
        Assert.False(settings.AutoFetchArtOnDiscRead);
        Assert.False(NetworkPreferences.NeedsArtworkAnswer(settings));
    }

    [Fact]
    public async Task AnAnsweredProfileIsNeverAskedAgain()
    {
        var settings = new AppSettings { AutoFetchArtAnswered = true, AutoFetchArtOnDiscRead = true };
        var prompt = new ScriptedPrompt(answer: false);

        Assert.True(await NetworkPreferences.AskAboutArtworkAsync(settings, prompt));

        Assert.Equal(0, prompt.Asked);
        Assert.True(settings.AutoFetchArtOnDiscRead);
    }

    [Fact]
    public async Task AHeadWithNoPromptCannotTurnItOn()
    {
        var settings = new AppSettings();

        Assert.False(await NetworkPreferences.AskAboutArtworkAsync(settings, prompt: null));

        Assert.False(settings.AutoFetchArtOnDiscRead);
        Assert.False(settings.AutoFetchArtAnswered);
        Assert.True(NetworkPreferences.NeedsArtworkAnswer(settings),
            "an unanswered question stays unanswered rather than becoming a no forever");
    }

    [Fact]
    public async Task TheQuestionSaysWhatIsContactedAndThatNothingIsUploaded()
    {
        var settings = new AppSettings();
        var prompt = new ScriptedPrompt(answer: true);

        await NetworkPreferences.AskAboutArtworkAsync(settings, prompt);

        Assert.Contains("Cover Art Archive", prompt.LastMessage);
        Assert.Contains("CUETools Database", prompt.LastMessage);
        Assert.Contains("nothing is uploaded", prompt.LastMessage);
        Assert.Contains("Settings", prompt.LastMessage);
        Assert.False(string.IsNullOrWhiteSpace(prompt.LastTitle));
    }
}
