using System.Text.Json.Serialization;

namespace CUETools.Linux.App.Services;

/// <summary>The manifest eng/build-cli-encoders.sh generates: one entry per
/// packaged command-line encoder with its SHA-256, recorded version lineage,
/// and license. The composition turns it into the catalog's packaged-encoder
/// trust table (D-047).</summary>
public sealed class LinuxEncoderManifest
{
    [JsonPropertyName("schemaVersion")] public int SchemaVersion { get; set; }
    [JsonPropertyName("encoders")] public List<LinuxEncoderEntry>? Encoders { get; set; }
}

public sealed class LinuxEncoderEntry
{
    [JsonPropertyName("file")] public string? File { get; set; }
    [JsonPropertyName("sha256")] public string? Sha256 { get; set; }
    [JsonPropertyName("bytes")] public long Bytes { get; set; }
    [JsonPropertyName("version")] public string? Version { get; set; }
    [JsonPropertyName("license")] public string? License { get; set; }
}

[JsonSerializable(typeof(LinuxEncoderManifest))]
public sealed partial class LinuxEncoderManifestJsonContext : JsonSerializerContext
{
}
