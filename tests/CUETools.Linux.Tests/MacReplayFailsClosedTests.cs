using CUETools.Linux.App.Journal;
using CUETools.Wpf.Services;
using Xunit;

namespace CUETools.Linux.Tests;

// The other half of ReplayPlatform's bargain: on a platform with no claim
// primitive (macOS today), replay must fail CLOSED - no claim, no replay, no
// entry touched - never race. This runs only on such a platform (the advisory
// macos CI lane); everywhere else the replay-behavior suites cover the claim.
public class MacReplayFailsClosedTests
{
    private sealed class CountingVerify : IVerifyService
    {
        public int Calls { get; private set; }
        public VerifyFilesResult Verify(string path, Action<double, string> progress)
        {
            Calls++;
            return new VerifyFilesResult { Ok = true, Status = "Verified.", Source = path };
        }
        public VerifyFilesResult Repair(string path, Action<double, string> progress)
            => throw new NotSupportedException();
    }

    [Fact]
    public void ReplayFailsClosedAndTouchesNoEntry()
    {
        if (!ReplayPlatform.Unsupported) return;

        string dir = Path.Combine(Path.GetTempPath(), $"backfill-mac-{Guid.NewGuid():N}");
        try
        {
            var store = new JournalStore(dir);
            store.CreatePending(BackfillLane.Verification, "/tmp/never-read.cue", "id");
            var inner = new CountingVerify();
            var backfill = new VerificationBackfillService(inner, store, isOnline: () => true);

            var outcome = backfill.ReplayPending();

            Assert.Equal(new VerificationBackfillService.ReplayOutcome(0, 0, 0), outcome);
            Assert.Equal(0, inner.Calls);
            BackfillJournalEntry entry = Assert.Single(store.ReadAll(out _));
            Assert.Equal(BackfillState.Pending, entry.State);
            Assert.Equal(0, entry.Attempts);
            Assert.Null(entry.LastAttemptUtc);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }
}
