using CUETools.Linux.App.Journal;
using CUETools.Wpf.Services;
using Xunit;

namespace CUETools.Linux.Tests;

public class BackfillTests
{
    private sealed class ScriptedVerifyService : IVerifyService
    {
        public Func<string, VerifyFilesResult> OnVerify = path =>
            new VerifyFilesResult { Ok = true, Status = "Verified.", Source = path, TocId = "TOC-1" };

        public int VerifyCalls { get; private set; }

        public VerifyFilesResult Verify(string path, Action<double, string> progress)
        {
            VerifyCalls++;
            return OnVerify(path);
        }

        public VerifyFilesResult Repair(string path, Action<double, string> progress)
            => throw new NotSupportedException();
    }

    private static JournalStore NewStore(out string dir)
    {
        dir = Path.Combine(Path.GetTempPath(), $"cuetools-backfill-test-{Guid.NewGuid():N}");
        return new JournalStore(dir);
    }

    [Fact]
    public void OfflineVerifyJournalsAndAnnotates()
    {
        var store = NewStore(out string dir);
        try
        {
            var service = new JournalingVerifyService(
                new ScriptedVerifyService(), store, isOnline: () => false);
            string lastStatus = "";
            VerifyFilesResult result = service.Verify(
                "/music/a.cue", (_, status) => lastStatus = status);

            Assert.True(result.Ok);
            Assert.Contains("queued for automatic backfill", lastStatus);
            BackfillJournalEntry entry = Assert.Single(store.ReadPending(BackfillLane.Verification));
            Assert.Equal("/music/a.cue", entry.SourcePath);
            Assert.Equal("TOC-1", entry.DiscId);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void OnlineVerifyDoesNotJournal()
    {
        var store = NewStore(out string dir);
        try
        {
            var service = new JournalingVerifyService(
                new ScriptedVerifyService(), store, isOnline: () => true);
            string lastStatus = "";
            VerifyFilesResult result = service.Verify(
                "/music/a.cue", (_, status) => lastStatus = status);

            Assert.True(result.Ok);
            Assert.DoesNotContain("backfill", lastStatus);
            Assert.Empty(store.ReadPending(BackfillLane.Verification));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void FailedVerifyIsNotJournaledEvenOffline()
    {
        var store = NewStore(out string dir);
        try
        {
            var inner = new ScriptedVerifyService
            {
                OnVerify = path => new VerifyFilesResult { Error = "bad cue", Source = path },
            };
            var service = new JournalingVerifyService(inner, store, isOnline: () => false);
            VerifyFilesResult result = service.Verify("/music/broken.cue", (_, _) => { });

            Assert.False(result.Ok);
            Assert.Empty(store.ReadPending(BackfillLane.Verification));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ReplaySkipsEntirelyWhenStillOffline()
    {
        var store = NewStore(out string dir);
        try
        {
            store.CreatePending(BackfillLane.Verification, "/music/a.cue", "id");
            var inner = new ScriptedVerifyService();
            var backfill = new VerificationBackfillService(inner, store, isOnline: () => false);

            var outcome = backfill.ReplayPending();
            Assert.Equal(0, outcome.Resolved);
            Assert.Equal(1, outcome.StillPending);
            Assert.Equal(0, inner.VerifyCalls);
            Assert.Equal(0, Assert.Single(store.ReadAll(out _)).Attempts);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ReplayResolvesExistingSourceAndRecordsEvidence()
    {
        var store = NewStore(out string dir);
        string album = Path.Combine(Path.GetTempPath(), $"cuetools-album-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(album);
            string cue = Path.Combine(album, "x.cue");
            File.WriteAllText(cue, "FILE \"x.wav\" WAVE");
            string report = Path.Combine(album, "x.accurip");

            store.CreatePending(BackfillLane.Verification, cue, "id");
            var inner = new ScriptedVerifyService
            {
                OnVerify = path =>
                {
                    File.WriteAllText(report, "dated report");
                    return new VerifyFilesResult { Ok = true, Status = "Verified.", Source = path };
                },
            };
            var backfill = new VerificationBackfillService(inner, store, isOnline: () => true);

            var outcome = backfill.ReplayPending();
            Assert.Equal(1, outcome.Resolved);
            BackfillJournalEntry entry = Assert.Single(store.ReadAll(out _));
            Assert.Equal(BackfillState.Resolved, entry.State);
            Assert.Equal(1, entry.Attempts);
            Assert.Equal(report, entry.ResolutionEvidencePath);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
            Directory.Delete(album, recursive: true);
        }
    }

    [Fact]
    public void ReplayMarksMissingSourceUnresolvableWithReason()
    {
        var store = NewStore(out string dir);
        try
        {
            store.CreatePending(
                BackfillLane.Verification, "/nonexistent/path/x.cue", "id");
            var inner = new ScriptedVerifyService();
            var backfill = new VerificationBackfillService(inner, store, isOnline: () => true);

            var outcome = backfill.ReplayPending();
            Assert.Equal(1, outcome.Unresolvable);
            Assert.Equal(0, inner.VerifyCalls);
            BackfillJournalEntry entry = Assert.Single(store.ReadAll(out _));
            Assert.Equal(BackfillState.Unresolvable, entry.State);
            Assert.False(string.IsNullOrEmpty(entry.Reason));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ReplayKeepsFailedVerifyPendingWithAttemptRecorded()
    {
        var store = NewStore(out string dir);
        string album = Path.Combine(Path.GetTempPath(), $"cuetools-album-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(album);
            string cue = Path.Combine(album, "x.cue");
            File.WriteAllText(cue, "FILE \"x.wav\" WAVE");

            store.CreatePending(BackfillLane.Verification, cue, "id");
            var inner = new ScriptedVerifyService
            {
                OnVerify = path => new VerifyFilesResult { Error = "decoder failed", Source = path },
            };
            var backfill = new VerificationBackfillService(inner, store, isOnline: () => true);

            var outcome = backfill.ReplayPending();
            Assert.Equal(1, outcome.StillPending);
            BackfillJournalEntry entry = Assert.Single(store.ReadAll(out _));
            Assert.Equal(BackfillState.Pending, entry.State);
            Assert.Equal(1, entry.Attempts);
            Assert.NotNull(entry.LastAttemptUtc);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
            Directory.Delete(album, recursive: true);
        }
    }
}
