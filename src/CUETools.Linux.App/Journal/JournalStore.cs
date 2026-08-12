using System.Text.Json;

namespace CUETools.Linux.App.Journal;

/// <summary>
/// File-backed store for backfill journal entries: one JSON document per
/// entry under the XDG state directory, written atomically (temp file +
/// rename). Reads are forward-compatible: entries with a newer
/// FormatVersion or unparseable content are surfaced as unreadable rather
/// than deleted, honoring the append-only evidence guardrail. JSON runs
/// through the source-generated context, so the store works under NativeAOT
/// where reflection serialization is disabled.
/// </summary>
public sealed class JournalStore
{
    private readonly string _directory;

    public JournalStore(string? directory = null)
    {
        // XDG_STATE_HOME (default ~/.local/state) is the right home for
        // "state that should persist but is not configuration".
        string stateRoot = Environment.GetEnvironmentVariable("XDG_STATE_HOME") is { Length: > 0 } xdg
            ? xdg
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "state");
        _directory = directory ?? Path.Combine(stateRoot, "cuetools-linux", "journal");
    }

    public string Directory => _directory;

    public BackfillJournalEntry CreatePending(BackfillLane lane, string sourcePath, string discId)
    {
        DateTime now = DateTime.UtcNow;
        var entry = new BackfillJournalEntry
        {
            Id = BackfillJournalEntry.NewId(now),
            Lane = lane,
            SourcePath = sourcePath,
            DiscId = discId,
            CreatedUtc = now,
            State = BackfillState.Pending,
        };
        Write(entry);
        return entry;
    }

    public void Update(BackfillJournalEntry entry) => Write(entry);

    /// <summary>All readable entries, oldest first (ids are creation-sorted).</summary>
    public IReadOnlyList<BackfillJournalEntry> ReadAll(out IReadOnlyList<string> unreadablePaths)
    {
        var entries = new List<BackfillJournalEntry>();
        var unreadable = new List<string>();
        if (System.IO.Directory.Exists(_directory))
        {
            foreach (string path in System.IO.Directory.GetFiles(_directory, "*.json").OrderBy(p => p, StringComparer.Ordinal))
            {
                try
                {
                    using FileStream stream = File.OpenRead(path);
                    BackfillJournalEntry? entry = JsonSerializer.Deserialize(
                        stream, JournalJsonContext.Default.BackfillJournalEntry);
                    if (entry == null || entry.FormatVersion > BackfillJournalEntry.CurrentFormatVersion)
                    {
                        unreadable.Add(path);
                        continue;
                    }
                    entries.Add(entry);
                }
                catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
                {
                    unreadable.Add(path);
                }
            }
        }
        unreadablePaths = unreadable;
        return entries;
    }

    public IReadOnlyList<BackfillJournalEntry> ReadPending(BackfillLane lane)
        => ReadAll(out _).Where(e => e.State == BackfillState.Pending && e.Lane == lane).ToList();

    private void Write(BackfillJournalEntry entry)
    {
        if (string.IsNullOrEmpty(entry.Id))
            throw new ArgumentException("Journal entry has no id.");
        if (entry.State == BackfillState.Unresolvable && string.IsNullOrEmpty(entry.Reason))
            throw new ArgumentException("Unresolvable journal entries require a reason.");

        System.IO.Directory.CreateDirectory(_directory);
        string path = Path.Combine(_directory, entry.Id + ".json");
        string temp = path + ".tmp";
        using (FileStream stream = new(temp, FileMode.Create, FileAccess.Write))
        {
            JsonSerializer.Serialize(stream, entry, JournalJsonContext.Default.BackfillJournalEntry);
            stream.Flush(flushToDisk: true);
        }
        File.Move(temp, path, overwrite: true);
    }
}
