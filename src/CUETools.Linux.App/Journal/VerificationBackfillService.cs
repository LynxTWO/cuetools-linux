using CUETools.Wpf.Services;

namespace CUETools.Linux.App.Journal;

/// <summary>
/// Replays pending verification journal entries once the databases are
/// reachable again. A replay re-runs the full verification, which writes a
/// fresh dated report next to the album (the engine's own writer); the
/// original offline report is never modified. Sources that no longer exist
/// become Unresolvable with a reason, never silently dropped. Even a
/// "not in database" answer resolves the entry: the databases were asked
/// and their dated answer is on disk.
/// </summary>
public sealed class VerificationBackfillService
{
    private readonly IVerifyService _verify;
    private readonly JournalStore _journal;
    private readonly Func<bool> _isOnline;

    public VerificationBackfillService(
        IVerifyService verify, JournalStore journal, Func<bool>? isOnline = null)
    {
        _verify = verify;
        _journal = journal;
        _isOnline = isOnline ?? ConnectivityProbe.IsOnline;
    }

    public sealed record ReplayOutcome(int Resolved, int Unresolvable, int StillPending);

    /// <summary>
    /// One replay at a time across every CUETools process. Without this, two windows opened
    /// together read the same pending entries and re-verify the same albums concurrently,
    /// each overwriting the other's fresh report. The optical drive has had cross-process
    /// exclusion since D-068; the journal had none.
    ///
    /// Returns null when another process holds the replay, which is not an error: that
    /// process is doing the work.
    /// </summary>
    private FileStream? TryClaimReplay()
    {
        // Both platforms enforce exclusion here: Windows through the share mode, Linux
        // through the advisory lock (FileStream.Lock is unsupported on macOS, so that
        // platform gets no claim and no replay rather than a silent race).
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
            return null;
        try
        {
            string path = Path.Combine(_journal.Directory, "replay.lock");
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var stream = new FileStream(
                path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            if (OperatingSystem.IsLinux())
            {
                try { stream.Lock(0, 1); }
                catch (IOException) { stream.Dispose(); return null; }
            }
            return stream;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public ReplayOutcome ReplayPending(Action<string>? log = null)
    {
        using FileStream? claim = TryClaimReplay();
        if (claim == null)
        {
            log?.Invoke("backfill: another CUETools process is replaying; skipped");
            return new ReplayOutcome(0, 0, 0);
        }

        IReadOnlyList<BackfillJournalEntry> pending =
            _journal.ReadPending(BackfillLane.Verification);
        if (pending.Count == 0)
            return new ReplayOutcome(0, 0, 0);

        if (!_isOnline())
        {
            log?.Invoke($"backfill: {pending.Count} entries pending, still offline");
            return new ReplayOutcome(0, 0, pending.Count);
        }

        int resolved = 0, unresolvable = 0, stillPending = 0;
        foreach (BackfillJournalEntry entry in pending)
        {
            entry.Attempts++;
            entry.LastAttemptUtc = DateTime.UtcNow;

            if (!File.Exists(entry.SourcePath) && !Directory.Exists(entry.SourcePath))
            {
                entry.State = BackfillState.Unresolvable;
                entry.Reason = "The journaled source no longer exists at its recorded path.";
                _journal.Update(entry);
                unresolvable++;
                log?.Invoke($"backfill: {entry.Id} unresolvable (source missing)");
                continue;
            }

            // The engine rewrites the album's report file on re-verify, and
            // evidence is append-only (ADD guardrail 5): snapshot the
            // offline-era report byte-for-byte before replaying, so history
            // survives beside the fresh dated report.
            string? priorReport = ReportFor(entry.SourcePath);
            if (priorReport != null)
            {
                string preserved = priorReport + "." +
                    DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".pre-backfill";
                try
                {
                    File.Copy(priorReport, preserved, overwrite: false);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    log?.Invoke($"backfill: {entry.Id} could not preserve prior report ({ex.GetType().Name})");
                }
            }

            VerifyFilesResult result = _verify.Verify(entry.SourcePath, (_, _) => { });
            // A completed run is not an answer. When neither database answered,
            // the fresh report records only a connection error, and resolving on
            // Ok alone consumed the entry with nothing learned - the exact case
            // the queue exists for. The connectivity probe runs once for the whole
            // batch, so a network that drops mid-replay lands here.
            if (result.Ok && result.ArLookupFailed && result.CtdbLookupFailed)
            {
                _journal.Update(entry);
                stillPending++;
                log?.Invoke($"backfill: {entry.Id} retry later (no database answered)");
            }
            else if (result.Ok)
            {
                entry.State = BackfillState.Resolved;
                entry.ResolutionEvidencePath = ReportFor(entry.SourcePath);
                _journal.Update(entry);
                resolved++;
                log?.Invoke($"backfill: {entry.Id} resolved");
            }
            else
            {
                // The verify itself failed (unreadable files, engine error):
                // keep it pending so a later run can retry, with the attempt
                // recorded.
                _journal.Update(entry);
                stillPending++;
                log?.Invoke($"backfill: {entry.Id} retry later (verify failed)");
            }
        }
        return new ReplayOutcome(resolved, unresolvable, stillPending);
    }

    /// <summary>
    /// The report belonging to THIS entry, which the engine names after the source stem
    /// (`ArLogFilenameFormat` is `%filename%.accurip`), so `album.cue` gives `album.accurip`.
    ///
    /// This used to return the newest `*.accurip` anywhere in the folder. A multi-disc album
    /// in one folder hit that every time: the snapshot preserved another disc's report while
    /// the replayed disc's own report was overwritten with no copy, and the entry then
    /// recorded the wrong file as its evidence. Silent evidence loss in the one path whose
    /// job is preserving evidence.
    ///
    /// A legacy entry naming a folder has no stem to work from, so it is only unambiguous
    /// when the folder holds exactly one report.
    /// </summary>
    private static string? ReportFor(string sourcePath)
    {
        try
        {
            if (Directory.Exists(sourcePath))
            {
                string[] reports = Directory.GetFiles(sourcePath, "*.accurip");
                return reports.Length == 1 ? reports[0] : null;
            }
            string? dir = Path.GetDirectoryName(sourcePath);
            if (dir == null || !Directory.Exists(dir))
                return null;
            string candidate = Path.Combine(
                dir, Path.GetFileNameWithoutExtension(sourcePath) + ".accurip");
            return File.Exists(candidate) ? candidate : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
