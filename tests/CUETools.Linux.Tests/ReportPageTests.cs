using CUETools.Wpf.Models;
using CUETools.Wpf.Services;
using CUETools.Wpf.ViewModels;
using Xunit;

namespace CUETools.Linux.Tests;

// The Report page over the shared store. The headline rules are R109's surface: the
// certificate must never say more than the log, damage outranks every verification
// claim, and Test and Copy agreement is never presented as a database match.
public class ReportPageTests
{
    private sealed class NullLog : IDiagnosticLog
    {
        public string LogPath => "";
        public void Info(string c, string m) { }
        public void Warn(string c, string m) { }
        public void Error(string c, string m, Exception? e = null) { }
        public void Redact(params string?[] s) { }
    }

    private static (ReportViewModel vm, ReportStore store) Create()
    {
        var store = new ReportStore(new NullLog());
        return (new ReportViewModel(store), store);
    }

    private static RipReport Make(
        bool accurate = false, int arConf = 0, int arTotal = 0,
        int failedWindows = 0, bool salvaged = false,
        int reads = 0, int agreeing = 0) => new()
    {
        Mode = "Rip",
        Album = "Aja",
        Artist = "Steely Dan",
        CorrectionQuality = 1,
        Accurate = accurate,
        ArConfidence = arConf,
        ArTotal = arTotal,
        FailedWindows = failedWindows,
        Salvaged = salvaged,
        OpticalReadsUsed = reads,
        MinimumAgreeingReads = agreeing,
    };

    [Fact]
    public void EmptyStoreShowsTheEmptyState()
    {
        var (vm, _) = Create();
        Assert.False(vm.HasReport);
        Assert.Equal("", vm.Headline);
    }

    [Fact]
    public void PublishingRefreshesThePageThroughTheSharedStore()
    {
        var (vm, store) = Create();
        store.Publish(Make(accurate: true, arConf: 4, arTotal: 4));
        Assert.True(vm.HasReport);
        Assert.Equal("Accurately ripped", vm.Headline);
        Assert.True(vm.Confirmed);
        Assert.Contains("confidence 4", vm.ArText);
        Assert.NotEqual("", vm.DigestFooter);
    }

    [Fact]
    public void DamageOutranksEveryVerificationHeadline()
    {
        var (vm, store) = Create();
        // Database-confirmed AND damaged: the certificate must lead with the damage.
        store.Publish(Make(accurate: true, arConf: 4, arTotal: 4, failedWindows: 2));
        Assert.Equal("Consistent - damage recorded", vm.Headline);
        Assert.False(vm.Confirmed);
    }

    [Fact]
    public void ASalvagedCaptureSaysSo()
    {
        var (vm, store) = Create();
        store.Publish(Make(salvaged: true, failedWindows: 1));
        Assert.Equal("Salvaged capture - damage recorded", vm.Headline);

        store.Publish(Make(salvaged: true));
        Assert.Equal("Salvaged capture - not verified", vm.Headline);
    }

    [Fact]
    public void IndependentReadsAreNamedAsThemselvesNotAsADatabaseMatch()
    {
        var (vm, store) = Create();
        store.Publish(Make(reads: 2, agreeing: 2));
        Assert.Equal("Verified by independent reads", vm.Headline);
        // The integrity line separates the read evidence from database confirmation.
        Assert.Contains("not found in AR/CTDB", vm.IntegrityLine);
    }

    [Fact]
    public void TheDigestIsReproducibleOverTheDisplayedBody()
    {
        var (vm, store) = Create();
        store.Publish(Make(accurate: true, arConf: 3, arTotal: 3));
        string digest = LogIntegrity.ComputeDigest(vm.LogBody);
        Assert.Equal(LogIntegrity.Footer(digest), vm.DigestFooter);
    }
}
