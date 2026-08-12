using System.Text.Json.Serialization;

namespace CUETools.Linux.App.Journal;

public enum BackfillLane
{
    Verification,
    Enrichment,
}

public enum BackfillState
{
    Pending,
    Resolved,
    Unresolvable,
}

/// <summary>
/// One pending network-dependent follow-up for a completed local job, per
/// the EDD section 5 data model. Format changes are stop-list controlled
/// (D-028): bump FormatVersion and keep reads forward-compatible.
/// </summary>
public sealed class BackfillJournalEntry
{
    public const int CurrentFormatVersion = 1;

    [JsonPropertyName("formatVersion")]
    public int FormatVersion { get; set; } = CurrentFormatVersion;

    /// <summary>Sortable unique id: UTC ticks (hex, 16 digits) + GUID. A
    /// ULID-equivalent without a package dependency; lexicographic order is
    /// creation order.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    /// <summary>The verified source path (cue sheet, folder, or file).</summary>
    [JsonPropertyName("sourcePath")]
    public string SourcePath { get; set; } = "";

    /// <summary>CDTOC identity of the disc, when known.</summary>
    [JsonPropertyName("discId")]
    public string DiscId { get; set; } = "";

    [JsonPropertyName("lane")]
    public BackfillLane Lane { get; set; }

    [JsonPropertyName("createdUtc")]
    public DateTime CreatedUtc { get; set; }

    [JsonPropertyName("state")]
    public BackfillState State { get; set; } = BackfillState.Pending;

    [JsonPropertyName("attempts")]
    public int Attempts { get; set; }

    [JsonPropertyName("lastAttemptUtc")]
    public DateTime? LastAttemptUtc { get; set; }

    /// <summary>Path of the evidence produced by a successful replay.</summary>
    [JsonPropertyName("resolutionEvidencePath")]
    public string? ResolutionEvidencePath { get; set; }

    /// <summary>Required when State is Unresolvable.</summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    public static string NewId(DateTime utcNow)
        => utcNow.Ticks.ToString("x16") + "-" + Guid.NewGuid().ToString("N");
}

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(BackfillJournalEntry))]
internal partial class JournalJsonContext : JsonSerializerContext
{
}
