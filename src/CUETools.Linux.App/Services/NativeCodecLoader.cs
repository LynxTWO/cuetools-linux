using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using CUETools.Codecs;

namespace CUETools.Linux.App.Services;

/// <summary>
/// The Linux head's counterpart to the Windows PluginTrustManifest for
/// native codec libraries (D-042): every packaged .so is hash-validated
/// against the packaged manifest BEFORE its exact absolute path is
/// registered with NativeDependencyPathRegistry and loaded. There is no
/// app-root, PATH, or bare-name fallback: a library that fails validation
/// simply leaves its codec unregistered, with the reason logged.
/// </summary>
public sealed class NativeCodecLoader
{
    public sealed record LibraryState(string File, bool Ready, string Detail);

    private readonly Dictionary<string, IntPtr> _handles = new(StringComparer.Ordinal);
    private readonly List<LibraryState> _states = new();

    public IReadOnlyList<LibraryState> States => _states;

    public bool IsReady(string dllName) => _handles.ContainsKey(dllName);

    /// <summary>Validate and load every manifest library from the packaged
    /// native directory. Failures are per-library and non-fatal.</summary>
    public void LoadAll(Action<string> log)
    {
        string nativeDir = Path.Combine(AppContext.BaseDirectory, "native");
        string manifestPath = Path.Combine(nativeDir, "native-codecs.json");
        if (!File.Exists(manifestPath))
        {
            log("no native codec manifest; native codecs stay unregistered");
            return;
        }

        NativeCodecManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize(
                File.ReadAllBytes(manifestPath),
                NativeCodecManifestJsonContext.Default.NativeCodecManifest);
        }
        catch (Exception ex)
        {
            log("native codec manifest unreadable: " + ex.GetType().Name);
            return;
        }

        if (manifest?.Libraries == null)
        {
            log("native codec manifest empty; native codecs stay unregistered");
            return;
        }

        foreach (NativeCodecLibrary library in manifest.Libraries)
        {
            string file = library.File ?? "";
            string path = Path.Combine(nativeDir, file);
            try
            {
                if (string.IsNullOrWhiteSpace(file) || file != Path.GetFileName(file))
                    throw new InvalidDataException("manifest entry is not one file name");
                byte[] bytes = File.ReadAllBytes(path);
                string actual = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
                if (!string.Equals(actual, library.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("hash mismatch");

                NativeDependencyPathRegistry.RegisterHostValidatedPath(file, path);
                IntPtr handle = NativeLibrary.Load(path);
                string dllName = Path.GetFileNameWithoutExtension(file);
                _handles[dllName] = handle;
                _states.Add(new LibraryState(file, true, "validated and loaded"));
                log($"native codec ready: {file} ({bytes.Length} bytes)");
            }
            catch (Exception ex)
            {
                _states.Add(new LibraryState(file, false, ex.Message));
                log($"native codec unavailable: {file} - {ex.Message}");
            }
        }
    }

    private static readonly string[] KnownDllNames =
        { "libFLAC_dynamic", "wavpackdll", "MACLibDll" };

    /// <summary>Bind a wrapper assembly's DllImport names to the validated,
    /// already-loaded libraries. A KNOWN codec name that failed validation
    /// throws instead of falling back, so the default loader never probes
    /// for it by bare name; unrelated names use default resolution.</summary>
    public void BindResolver(Assembly wrapperAssembly)
    {
        NativeLibrary.SetDllImportResolver(wrapperAssembly, (name, _, _) =>
        {
            if (_handles.TryGetValue(name, out IntPtr handle)) return handle;
            if (Array.IndexOf(KnownDllNames, name) >= 0)
                throw new DllNotFoundException(
                    name + " is not validated for this installation.");
            return IntPtr.Zero;
        });
    }
}

public sealed class NativeCodecManifest
{
    [JsonPropertyName("schemaVersion")] public int SchemaVersion { get; set; }
    [JsonPropertyName("libraries")] public List<NativeCodecLibrary>? Libraries { get; set; }
}

public sealed class NativeCodecLibrary
{
    [JsonPropertyName("file")] public string? File { get; set; }
    [JsonPropertyName("sha256")] public string? Sha256 { get; set; }
    [JsonPropertyName("bytes")] public long Bytes { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(NativeCodecManifest))]
public sealed partial class NativeCodecManifestJsonContext : JsonSerializerContext
{
}
