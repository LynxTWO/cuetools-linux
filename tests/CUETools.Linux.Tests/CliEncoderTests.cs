using System.Diagnostics;
using CUETools.Linux.App;
using CUETools.Wpf.Services;
using Xunit;

namespace CUETools.Linux.Tests;

// SLICE-007 increment B (D-047): the curated command-line encoders ship as
// pinned-source ELF builds under the catalog's cross-head identities
// (opusenc.exe, oggenc.exe, mpcenc.exe are ELF binaries - the name is
// identity, not a format claim). Health means the executable actually runs;
// a byte that does not match the packaged manifest is refused.
public class CliEncoderTests
{
    private static string EncodersDir =>
        Path.Combine(AppContext.BaseDirectory, "encoders");

    [Theory]
    [InlineData("opusenc.exe", "opus-tools")]
    [InlineData("oggenc.exe", "vorbis-tools")]
    [InlineData("mpcenc.exe", "r495")]
    public void PackagedEncoderExecutesAndReportsItsPinnedLineage(string file, string marker)
    {
        string path = Path.Combine(EncodersDir, file);
        Assert.True(File.Exists(path), $"{file} missing - run eng/build-cli-encoders.sh");

        var startInfo = new ProcessStartInfo
        {
            FileName = path,
            Arguments = file == "mpcenc.exe" ? "" : "--version",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using Process process = Process.Start(startInfo)!;
        string output = process.StandardOutput.ReadToEnd() +
            process.StandardError.ReadToEnd();
        process.WaitForExit(10000);
        Assert.Contains(marker, output);
    }

    [Fact]
    public void ManifestHashesMatchTheShippedBytes()
    {
        string manifestPath = Path.Combine(EncodersDir, "linux-encoders.json");
        Assert.True(File.Exists(manifestPath), "linux-encoders.json missing");
        var manifest = System.Text.Json.JsonSerializer.Deserialize(
            File.ReadAllBytes(manifestPath),
            CUETools.Linux.App.Services.LinuxEncoderManifestJsonContext.Default.LinuxEncoderManifest);
        Assert.NotNull(manifest?.Encoders);
        Assert.Equal(3, manifest!.Encoders!.Count);
        foreach (var entry in manifest.Encoders)
        {
            byte[] bytes = File.ReadAllBytes(Path.Combine(EncodersDir, entry.File!));
            string actual = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
            Assert.Equal(entry.Sha256, actual);
            Assert.False(string.IsNullOrWhiteSpace(entry.License));
            Assert.False(string.IsNullOrWhiteSpace(entry.Version));
        }
    }

    [Fact]
    public void TamperedBundledEncoderIsRefusedByTheCatalog()
    {
        // A bundled directory whose binary does not match the packaged table
        // must not resolve - falling through to it would run bytes nobody
        // approved. Uses an isolated bundled dir; the real layout is
        // untouched.
        string dir = Path.Combine(
            Path.GetTempPath(), $"cuetools-encoders-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "mpcenc.exe"), "#!/bin/sh\nexit 0\n");
            var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["mpcenc.exe"] = new string('0', 64), // deliberately wrong
            };
            var catalog = new EncoderCatalog(
                new NullLog(), new AppSettings(), dir, hashes);
            var config = Composition.CreateDefaultConfig();
            catalog.EnsureRegistered(config);
            var mpc = catalog.BuildChoices(config)
                .FirstOrDefault(choice => choice.Extension == "mpc");
            if (mpc != null)
            {
                Assert.False(mpc.CanSelect,
                    "a hash-mismatched bundled encoder must not be selectable");
            }
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
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
}
