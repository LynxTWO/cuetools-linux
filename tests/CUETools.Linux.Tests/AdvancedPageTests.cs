using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using CUETools.Linux.App;
using CUETools.Linux.App.Views;
using CUETools.Processor;
using CUETools.Wpf.Services;
using CUETools.Wpf.ViewModels;
using Xunit;

namespace CUETools.Linux.Tests;

// The Advanced page. The view model is a straight property map over
// CUEConfigAdvanced (the engine already honors every key); these pin the
// head's wiring and the one guarded path, the proxy credential.
public class AdvancedPageTests
{
    private sealed class RecordingLog : IDiagnosticLog
    {
        public readonly List<string> Redacted = new();
        public string LogPath => "/tmp/nowhere.log";
        public void Info(string category, string message) { }
        public void Warn(string category, string message) { }
        public void Error(string category, string message, Exception? ex = null) { }
        public void Redact(params string?[] sensitive)
        {
            foreach (string? s in sensitive) if (!string.IsNullOrEmpty(s)) Redacted.Add(s);
        }
    }

    [AvaloniaFact]
    public void ThePageMaterializesAndEditsReachTheLiveConfig()
    {
        var config = Composition.CreateDefaultConfig();
        var vm = new AdvancedViewModel(config, new RecordingLog());
        var window = new Window { Content = new AdvancedView { DataContext = vm } };
        window.Show();

        vm.CreateTOC = true;
        Assert.True(config.advanced.CreateTOC);
        vm.MetadataSearch = CUETools.CTDB.CTDBMetadataSearch.Extensive;
        Assert.Equal(CUETools.CTDB.CTDBMetadataSearch.Extensive, config.advanced.metadataSearch);
        window.Close();
    }

    [Fact]
    public void TheProxyCredentialIsRedactedOnSetAndNeverEchoed()
    {
        var config = Composition.CreateDefaultConfig();
        var log = new RecordingLog();
        var vm = new AdvancedViewModel(config, log);

        Assert.False(vm.HasProxyPassword);
        Assert.Equal("No credential", vm.ProxyPasswordStatus);
        Assert.False(vm.SetProxyPassword(""));   // empty input is not a credential

        Assert.True(vm.SetProxyPassword("hunter2"));
        Assert.True(vm.HasProxyPassword);
        Assert.Equal("Credential set", vm.ProxyPasswordStatus);
        // the value goes to the log's redaction list, and the status line never carries it
        Assert.Contains("hunter2", log.Redacted);
        Assert.DoesNotContain("hunter2", vm.ProxyPasswordStatus);

        vm.ClearProxyPassword();
        Assert.False(vm.HasProxyPassword);
        Assert.Equal("", config.advanced.ProxyPassword);
    }
}
