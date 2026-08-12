using System.Net.Sockets;
using CUETools.Wpf.Services;

namespace CUETools.Linux.App.Journal;

/// <summary>
/// Decorates the engine-backed verify service with the offline backfill
/// journal (ADD section 3, D-010/D-011): when the verification databases are
/// unreachable at verify time, the local result stands untouched and a
/// pending verification entry is journaled for automatic replay. Detection
/// is a direct connectivity probe of both database endpoints; "offline"
/// requires both to be unreachable, so a single flaky service never queues
/// spurious backfill.
/// </summary>
public sealed class JournalingVerifyService : IVerifyService
{
    private readonly IVerifyService _inner;
    private readonly JournalStore _journal;
    private readonly Func<bool> _isOnline;

    public JournalingVerifyService(
        IVerifyService inner, JournalStore journal, Func<bool>? isOnline = null)
    {
        _inner = inner;
        _journal = journal;
        _isOnline = isOnline ?? ConnectivityProbe.IsOnline;
    }

    public VerifyFilesResult Verify(string path, Action<double, string> progress)
    {
        bool online = _isOnline();
        VerifyFilesResult result = _inner.Verify(path, progress);
        if (!online && result.Ok)
        {
            _journal.CreatePending(
                BackfillLane.Verification, path, result.TocId ?? "");
            progress(1,
                result.Status +
                " Offline: database verification queued for automatic backfill.");
        }
        return result;
    }

    public VerifyFilesResult Repair(string path, Action<double, string> progress)
        => _inner.Repair(path, progress);
}

/// <summary>
/// TCP reachability of the verification databases. Online means at least
/// one endpoint answers; the pair covers both providers so one outage does
/// not masquerade as being offline.
/// </summary>
public static class ConnectivityProbe
{
    private static readonly (string Host, int Port)[] Endpoints =
    {
        ("db.cuetools.net", 80),
        ("www.accuraterip.com", 443),
    };

    public static bool IsOnline()
    {
        foreach ((string host, int port) in Endpoints)
        {
            try
            {
                using var client = new TcpClient();
                if (client.ConnectAsync(host, port).Wait(TimeSpan.FromSeconds(3)))
                    return true;
            }
            catch
            {
                // Unreachable endpoint: try the next one.
            }
        }
        return false;
    }
}
