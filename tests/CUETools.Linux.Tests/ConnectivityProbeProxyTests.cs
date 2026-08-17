using System.Net;
using CUETools.Linux.App.Journal;
using Xunit;

namespace CUETools.Linux.Tests;

// F-27: the probe opened raw sockets to the database hosts while every real lookup went
// through the configured proxy. On a network that allows only proxied outbound, that reported
// offline for services the app could reach, so every verify was journaled and every replay
// failed the same way.
public class ConnectivityProbeProxyTests
{
    private sealed class RecordingProxy : IWebProxy
    {
        public RecordingProxy(Uri? hop) => _hop = hop;

        private readonly Uri? _hop;
        public List<Uri> Asked { get; } = new();
        public ICredentials? Credentials { get; set; }

        public Uri? GetProxy(Uri destination)
        {
            Asked.Add(destination);
            return _hop ?? destination;
        }

        public bool IsBypassed(Uri host) => _hop == null;
    }

    private sealed class ThrowingProxy : IWebProxy
    {
        public ICredentials? Credentials { get; set; }
        public Uri GetProxy(Uri destination) => throw new NotSupportedException("no route");
        public bool IsBypassed(Uri host) => false;
    }

    [Fact]
    public void TheProxyIsAskedAboutBothDatabaseEndpoints()
    {
        // Port 9 is discard: nothing listens, so the probe fails fast and we are only
        // checking which destinations the proxy was consulted about.
        var proxy = new RecordingProxy(new Uri("http://127.0.0.1:9"));

        bool online = ConnectivityProbe.IsOnline(proxy);

        Assert.False(online);
        Assert.Contains(proxy.Asked, uri => uri.Host == "db.cuetools.net");
        Assert.Contains(proxy.Asked, uri => uri.Host == "www.accuraterip.com");
        // The scheme has to match the port the app really uses, or a system proxy answers
        // for the wrong protocol.
        Assert.Contains(proxy.Asked, uri => uri.Scheme == "http" && uri.Host == "db.cuetools.net");
        Assert.Contains(proxy.Asked, uri => uri.Scheme == "https" && uri.Host == "www.accuraterip.com");
    }

    [Fact]
    public void AProxyThatCannotAnswerFallsThroughInsteadOfThrowing()
    {
        // A proxy that refuses to answer for a destination must not take the probe down with
        // it: the fall-through is to probe the destination directly. The return value depends
        // on this machine's real connectivity, so the contract under test is that the call
        // completes rather than what it decides.
        Exception? thrown = Record.Exception(() => ConnectivityProbe.IsOnline(new ThrowingProxy()));

        Assert.Null(thrown);
    }

    [Fact]
    public void NoProxyKeepsTheDirectProbeAndInventsNoDestinations()
    {
        var proxy = new RecordingProxy(hop: null);

        ConnectivityProbe.IsOnline(proxy);

        // The probe stops at the first endpoint it can reach, so the number of questions
        // depends on this machine's connectivity and must not be asserted. What must hold
        // either way: it asked about at least one endpoint, and only ever about the two the
        // app really uses.
        Assert.NotEmpty(proxy.Asked);
        Assert.All(
            proxy.Asked,
            uri => Assert.Contains(uri.Host, new[] { "db.cuetools.net", "www.accuraterip.com" }));
    }
}
