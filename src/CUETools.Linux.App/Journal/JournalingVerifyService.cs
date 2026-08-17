using System.Net;
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
            // Queue this album once, however many times it is verified offline.
            // Without the check, verifying the same album N times offline queued
            // it N times and the next online launch re-verified it N times. The
            // enrichment lane's comment claimed it shared "the same double-check
            // discipline as the verify lane", which was not true of this lane
            // until now.
            bool alreadyPending = _journal.ReadPending(BackfillLane.Verification)
                .Any(entry => string.Equals(entry.SourcePath, path, StringComparison.Ordinal));
            if (!alreadyPending)
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

    public static bool IsOnline() => IsOnline(null);

    /// <summary>
    /// Reachability of the two verification databases, as the app would reach them.
    ///
    /// The probe used to open raw sockets to the database hosts while every real lookup went
    /// through the configured proxy. On a network where direct outbound connections are
    /// blocked but a proxy works, that reported offline for a service the app could in fact
    /// reach: every verify was journaled, and the backfill then failed the same way forever.
    ///
    /// Passing the engine's proxy fixes that. For each endpoint, whatever the proxy says it
    /// would actually connect to is what gets probed, which is the destination itself when no
    /// proxy applies to it. One reachable endpoint is enough; the lookups themselves decide
    /// the rest.
    /// </summary>
    public static bool IsOnline(IWebProxy? proxy)
    {
        foreach ((string host, int port) in Endpoints)
        {
            string probeHost = host;
            int probePort = port;
            try
            {
                var destination = new Uri((port == 443 ? "https://" : "http://") + host);
                Uri? hop = proxy?.GetProxy(destination);
                if (hop != null && !hop.Equals(destination))
                {
                    probeHost = hop.Host;
                    probePort = hop.Port;
                }
            }
            catch
            {
                // A proxy that cannot answer for this destination is not a reason to call the
                // network down; fall through and probe the destination itself.
            }

            try
            {
                using var client = new TcpClient();
                if (client.ConnectAsync(probeHost, probePort).Wait(TimeSpan.FromSeconds(3)))
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
