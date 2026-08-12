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

    public ReplayOutcome ReplayPending(Action<string>? log = null)
    {
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
            string? priorReport = FindFreshReport(entry.SourcePath);
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
            if (result.Ok)
            {
                entry.State = BackfillState.Resolved;
                entry.ResolutionEvidencePath = FindFreshReport(entry.SourcePath);
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

    private static string? FindFreshReport(string sourcePath)
    {
        try
        {
            string? dir = Directory.Exists(sourcePath)
                ? sourcePath
                : Path.GetDirectoryName(sourcePath);
            if (dir == null || !Directory.Exists(dir))
                return null;
            return Directory.GetFiles(dir, "*.accurip")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
