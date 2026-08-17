using CUETools.Linux.App.Journal;
using CUETools.Wpf.Services;
using Xunit;

namespace CUETools.Linux.Tests;

// F-01, F-02, F-04 from the manual fact check: the backfill lane could preserve another
// disc's report, resolve an entry whose fresh report holds only a connection error, and
// queue one album many times over. All three lose or corrupt the record silently, in the
// one subsystem whose job is keeping it.
public class BackfillEvidenceTests
{
    private sealed class ScriptedVerify : IVerifyService
    {
        public Func<string, VerifyFilesResult> OnVerify = path => new()
        {
            Ok = true, Status = "Verified.", ArConfidence = 3, Accurate = true, Source = path,
        };

        public int Calls { get; private set; }

        public VerifyFilesResult Verify(string path, Action<double, string> progress)
        {
            Calls++;
            return OnVerify(path);
        }

        public VerifyFilesResult Repair(string path, Action<double, string> progress)
            => throw new NotSupportedException();
    }

    private static string NewDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"backfill-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static (JournalStore journal, string dir) NewJournal()
    {
        string dir = NewDir();
        return (new JournalStore(dir), dir);
    }

    [Fact]
    public void AMultiDiscFolderPreservesTheReplayedDiscsOwnReport()
    {
        string album = NewDir();
        (JournalStore journal, string journalDir) = NewJournal();
        try
        {
            // Two discs of one album in one folder, the layout that hit this every time.
            string disc1 = Path.Combine(album, "disc1.cue");
            string disc2 = Path.Combine(album, "disc2.cue");
            File.WriteAllText(disc1, "FILE \"01.wav\" WAVE\n");
            File.WriteAllText(disc2, "FILE \"02.wav\" WAVE\n");
            File.WriteAllText(Path.Combine(album, "disc1.accurip"), "disc one, offline era");
            string disc2Report = Path.Combine(album, "disc2.accurip");
            File.WriteAllText(disc2Report, "disc two, offline era");
            // Disc 2's report is the newest, which is what the old lookup would have found
            // regardless of which disc was being replayed.
            File.SetLastWriteTimeUtc(disc2Report, DateTime.UtcNow.AddMinutes(5));

            journal.CreatePending(BackfillLane.Verification, disc1, "toc-1");

            var service = new VerificationBackfillService(
                new ScriptedVerify(), journal, isOnline: () => true);
            service.ReplayPending();

            string[] preserved = Directory.GetFiles(album, "*.pre-backfill");
            string snapshot = Assert.Single(preserved);
            Assert.StartsWith("disc1.accurip", Path.GetFileName(snapshot));
            Assert.Equal("disc one, offline era", File.ReadAllText(snapshot));
        }
        finally
        {
            Directory.Delete(album, true);
            Directory.Delete(journalDir, true);
        }
    }

    [Fact]
    public void AnEntryStaysPendingWhenNoDatabaseAnswered()
    {
        string album = NewDir();
        (JournalStore journal, string journalDir) = NewJournal();
        try
        {
            string cue = Path.Combine(album, "album.cue");
            File.WriteAllText(cue, "FILE \"01.wav\" WAVE\n");
            journal.CreatePending(BackfillLane.Verification, cue, "toc");

            // The batch probe said online, then the network dropped: the run completes and
            // its report carries nothing but a connection error.
            var verify = new ScriptedVerify
            {
                OnVerify = path => new()
                {
                    Ok = true,
                    Status = "Verified.",
                    ArLookupFailed = true,
                    CtdbLookupFailed = true,
                    Source = path,
                },
            };

            var outcome = new VerificationBackfillService(verify, journal, isOnline: () => true)
                .ReplayPending();

            Assert.Equal(0, outcome.Resolved);
            Assert.Equal(1, outcome.StillPending);
            Assert.Single(journal.ReadPending(BackfillLane.Verification));
        }
        finally
        {
            Directory.Delete(album, true);
            Directory.Delete(journalDir, true);
        }
    }

    [Fact]
    public void AnEntryResolvesWhenADatabaseActuallyAnswered()
    {
        string album = NewDir();
        (JournalStore journal, string journalDir) = NewJournal();
        try
        {
            string cue = Path.Combine(album, "album.cue");
            File.WriteAllText(cue, "FILE \"01.wav\" WAVE\n");
            journal.CreatePending(BackfillLane.Verification, cue, "toc");

            // "Not in database" is a real answer and must still resolve the entry.
            var verify = new ScriptedVerify
            {
                OnVerify = path => new() { Ok = true, Status = "Verified.", Source = path },
            };

            var outcome = new VerificationBackfillService(verify, journal, isOnline: () => true)
                .ReplayPending();

            Assert.Equal(1, outcome.Resolved);
            Assert.Empty(journal.ReadPending(BackfillLane.Verification));
        }
        finally
        {
            Directory.Delete(album, true);
            Directory.Delete(journalDir, true);
        }
    }

    [Fact]
    public void VerifyingOneAlbumOfflineManyTimesQueuesItOnce()
    {
        string album = NewDir();
        (JournalStore journal, string journalDir) = NewJournal();
        try
        {
            string cue = Path.Combine(album, "album.cue");
            File.WriteAllText(cue, "FILE \"01.wav\" WAVE\n");

            var service = new JournalingVerifyService(
                new ScriptedVerify(), journal, isOnline: () => false);

            for (int i = 0; i < 4; i++)
                service.Verify(cue, (_, _) => { });

            Assert.Single(journal.ReadPending(BackfillLane.Verification));
        }
        finally
        {
            Directory.Delete(album, true);
            Directory.Delete(journalDir, true);
        }
    }

    [Fact]
    public void AReplayHeldByAnotherProcessIsSkippedRatherThanRaced()
    {
        // The claim has to be held by a DIFFERENT process to prove anything: POSIX advisory
        // locks belong to the process, so a second handle opened here would not conflict with
        // the first no matter what the code does. A child python3 holding fcntl is the same
        // probe this project already uses to inspect drive leases.
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("the advisory-lock path under test is Linux-only");
            return;
        }

        string album = NewDir();
        (JournalStore journal, string journalDir) = NewJournal();
        System.Diagnostics.Process? holder = null;
        try
        {
            string cue = Path.Combine(album, "album.cue");
            File.WriteAllText(cue, "FILE \"01.wav\" WAVE\n");
            journal.CreatePending(BackfillLane.Verification, cue, "toc");

            string claimPath = Path.Combine(journalDir, "replay.lock");
            string ready = Path.Combine(journalDir, "held.flag");
            string script =
                "import fcntl,sys,time,os\n" +
                $"f=open({ToPythonLiteral(claimPath)},'a+b')\n" +
                "fcntl.lockf(f,fcntl.LOCK_EX|fcntl.LOCK_NB,1,0)\n" +
                $"open({ToPythonLiteral(ready)},'w').close()\n" +
                "time.sleep(30)\n";
            holder = System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo("python3", new[] { "-c", script })
                {
                    RedirectStandardError = true,
                });
            if (holder == null)
            {
                Assert.Skip("python3 unavailable to hold the claim from another process");
                return;
            }
            for (int i = 0; i < 100 && !File.Exists(ready); i++) Thread.Sleep(50);
            if (!File.Exists(ready))
            {
                Assert.Skip("python3 could not take the lock: " + holder.StandardError.ReadToEnd());
                return;
            }

            var verify = new ScriptedVerify();
            var outcome = new VerificationBackfillService(verify, journal, isOnline: () => true)
                .ReplayPending();

            Assert.Equal(0, verify.Calls);
            Assert.Equal(0, outcome.Resolved + outcome.Unresolvable + outcome.StillPending);
            Assert.Single(journal.ReadPending(BackfillLane.Verification));
        }
        finally
        {
            try { holder?.Kill(); } catch (InvalidOperationException) { }
            holder?.Dispose();
            Directory.Delete(album, true);
            Directory.Delete(journalDir, true);
        }
    }

    [Fact]
    public void TheClaimIsReleasedSoTheNextReplayProceeds()
    {
        string album = NewDir();
        (JournalStore journal, string journalDir) = NewJournal();
        try
        {
            string cue = Path.Combine(album, "album.cue");
            File.WriteAllText(cue, "FILE \"01.wav\" WAVE\n");
            journal.CreatePending(BackfillLane.Verification, cue, "toc");

            var verify = new ScriptedVerify();
            var service = new VerificationBackfillService(verify, journal, isOnline: () => true);

            service.ReplayPending();
            journal.CreatePending(BackfillLane.Verification, cue, "toc");
            service.ReplayPending();

            Assert.Equal(2, verify.Calls);
        }
        finally
        {
            Directory.Delete(album, true);
            Directory.Delete(journalDir, true);
        }
    }

    private static string ToPythonLiteral(string path)
        => "'" + path.Replace("\\", "\\\\").Replace("'", "\\'") + "'";
}
