using CUETools.Processor;
using CUETools.Linux.App.Services;
using CUETools.Wpf.Services;
using Xunit;

namespace CUETools.Linux.Tests;

// SLICE-006 settings persistence (D-043): the shared SettingsStore round-
// trips CUEConfig and AppSettings on Linux through the engine's profile
// format, using an isolated portable-mode profile so the user's own
// settings file is never touched. Secrets are never persisted on this head
// (DecliningSecretProtector), and a profile carrying a foreign protected
// credential degrades to no-credential instead of a plaintext fallback.
public class SettingsPersistenceTests
{
    private sealed class NullLog : IDiagnosticLog
    {
        public string LogPath => "";
        public void Info(string category, string message) { }
        public void Warn(string category, string message) { }
        public void Error(string category, string message, Exception? ex = null) { }
        public void Redact(params string?[] sensitive) { }
    }

    private static (SettingsStore store, string dir, string fakeApp) CreateIsolatedStore()
    {
        // Portable-mode convention: appPath is a FILE path and the profile
        // lands in <its directory>/CUETools2026 (see SettingsShared).
        string dir = Path.Combine(
            Path.GetTempPath(), $"cuetools-settings-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        string fakeApp = Path.Combine(dir, "cuetools-linux");
        File.WriteAllText(fakeApp, "");
        var store = new SettingsStore(new NullLog(), fakeApp, new DecliningSecretProtector());
        return (store, dir, fakeApp);
    }

    [Fact]
    public void SettingsRoundTripAcrossStoreInstances()
    {
        var (store, dir, fakeApp) = CreateIsolatedStore();
        try
        {
            var config = new CUEConfig();
            var app = new AppSettings
            {
                OutputBaseDir = "/music/out",
                SelectedFormat = "ape",
                NamingTemplate = "%artist% - %album%",
                NamingHandleArticles = false,
            };
            config.createEACLOG = !config.createEACLOG;
            store.Save(config, app);
            Assert.True(File.Exists(store.SettingsFilePath), "settings file was not written");

            var freshStore = new SettingsStore(
                new NullLog(), fakeApp, new DecliningSecretProtector());
            var freshConfig = new CUEConfig();
            var freshApp = new AppSettings();
            freshStore.Load(freshConfig, freshApp);

            Assert.Equal("/music/out", freshApp.OutputBaseDir);
            Assert.Equal("ape", freshApp.SelectedFormat);
            Assert.Equal("%artist% - %album%", freshApp.NamingTemplate);
            Assert.False(freshApp.NamingHandleArticles);
            Assert.Equal(config.createEACLOG, freshConfig.createEACLOG);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void SecretsAreNeverPersistedOnThisHead()
    {
        var (store, dir, _) = CreateIsolatedStore();
        try
        {
            var config = new CUEConfig();
            config.advanced.ProxyPassword = "super-secret";
            store.Save(config, new AppSettings());

            string profile = File.ReadAllText(store.SettingsFilePath);
            Assert.DoesNotContain("super-secret", profile);
            Assert.DoesNotContain("WpfProxyPasswordProtected", profile);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ForeignProtectedCredentialDegradesToNoCredential()
    {
        var (store, dir, fakeApp) = CreateIsolatedStore();
        try
        {
            store.Save(new CUEConfig(), new AppSettings());
            File.AppendAllText(
                store.SettingsFilePath,
                "WpfProxyPasswordProtected=dpapi-v1:AAAA\n");

            var freshStore = new SettingsStore(
                new NullLog(), fakeApp, new DecliningSecretProtector());
            var config = new CUEConfig();
            config.advanced.ProxyPassword = "stale-plaintext";
            freshStore.Load(config, new AppSettings());

            Assert.Equal("", config.advanced.ProxyPassword);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
