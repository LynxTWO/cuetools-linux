using CUETools.Linux.App.Journal;
using Xunit;

namespace CUETools.Linux.Tests;

public class JournalStoreTests
{
    private static JournalStore NewStore(out string dir)
    {
        dir = Path.Combine(Path.GetTempPath(), $"cuetools-journal-test-{Guid.NewGuid():N}");
        return new JournalStore(dir);
    }

    [Fact]
    public void CreatePendingRoundTripsThroughDisk()
    {
        var store = NewStore(out string dir);
        try
        {
            BackfillJournalEntry created = store.CreatePending(
                BackfillLane.Verification, "/music/album/album.cue", "TOCID-abc");

            var entries = store.ReadAll(out var unreadable);
            Assert.Empty(unreadable);
            BackfillJournalEntry read = Assert.Single(entries);
            Assert.Equal(created.Id, read.Id);
            Assert.Equal(BackfillLane.Verification, read.Lane);
            Assert.Equal(BackfillState.Pending, read.State);
            Assert.Equal("/music/album/album.cue", read.SourcePath);
            Assert.Equal("TOCID-abc", read.DiscId);
            Assert.Equal(BackfillJournalEntry.CurrentFormatVersion, read.FormatVersion);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void IdsSortByCreationOrder()
    {
        string a = BackfillJournalEntry.NewId(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        string b = BackfillJournalEntry.NewId(new DateTime(2026, 1, 1, 0, 0, 1, DateTimeKind.Utc));
        Assert.True(string.CompareOrdinal(a, b) < 0);
    }

    [Fact]
    public void UpdateTransitionsStateAndPendingFilterHonorsIt()
    {
        var store = NewStore(out string dir);
        try
        {
            BackfillJournalEntry entry = store.CreatePending(
                BackfillLane.Verification, "/music/x.cue", "id");
            Assert.Single(store.ReadPending(BackfillLane.Verification));

            entry.State = BackfillState.Resolved;
            entry.Attempts = 1;
            entry.LastAttemptUtc = DateTime.UtcNow;
            entry.ResolutionEvidencePath = "/music/x.accurip";
            store.Update(entry);

            Assert.Empty(store.ReadPending(BackfillLane.Verification));
            BackfillJournalEntry read = Assert.Single(store.ReadAll(out _));
            Assert.Equal(BackfillState.Resolved, read.State);
            Assert.Equal("/music/x.accurip", read.ResolutionEvidencePath);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void UnresolvableRequiresReason()
    {
        var store = NewStore(out string dir);
        try
        {
            BackfillJournalEntry entry = store.CreatePending(
                BackfillLane.Verification, "/music/x.cue", "id");
            entry.State = BackfillState.Unresolvable;
            Assert.Throws<ArgumentException>(() => store.Update(entry));

            entry.Reason = "source files moved";
            store.Update(entry);
            Assert.Equal(BackfillState.Unresolvable, Assert.Single(store.ReadAll(out _)).State);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void NewerFormatVersionsAreUnreadableNotDeleted()
    {
        var store = NewStore(out string dir);
        try
        {
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "zzzz-future.json");
            File.WriteAllText(path, "{\"formatVersion\": 999, \"id\": \"zzzz-future\"}");

            var entries = store.ReadAll(out var unreadable);
            Assert.Empty(entries);
            Assert.Equal(path, Assert.Single(unreadable));
            Assert.True(File.Exists(path));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void CorruptEntriesAreSurfacedNotDeleted()
    {
        var store = NewStore(out string dir);
        try
        {
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "0000-corrupt.json");
            File.WriteAllText(path, "not json at all {");

            store.CreatePending(BackfillLane.Verification, "/music/y.cue", "id2");
            var entries = store.ReadAll(out var unreadable);
            Assert.Single(entries);
            Assert.Equal(path, Assert.Single(unreadable));
            Assert.True(File.Exists(path));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
