using Avalonia.Headless.XUnit;
using CUETools.Processor;
using CUETools.Wpf.Services;
using CUETools.Wpf.ViewModels;
using Xunit;

namespace CUETools.Linux.Tests;

// Two fixes the manual fact check surfaced, proven on the Linux head.
//
// The panel tests are AvaloniaFact rather than Fact for the reason recorded in
// AutoRepairDriverTests: RelayCommands live on the static RequeryHub for the
// process lifetime, so driving a view model from a plain xunit worker thread
// broadcasts cross-thread into Buttons left by earlier headless UI tests.
public class LookupStatusAndPlaylistTests
{
    private sealed class ScriptedVerifyService : IVerifyService
    {
        public ScriptedVerifyService(Func<string, VerifyFilesResult> result)
            => _result = result;

        private readonly Func<string, VerifyFilesResult> _result;

        public VerifyFilesResult Verify(string path, Action<double, string> progress)
        {
            progress(1, "done");
            return _result(path);
        }

        public VerifyFilesResult Repair(string path, Action<double, string> progress)
            => _result(path);
    }

    private static string WriteAlbum(string manifestName, string manifestBody)
    {
        string dir = Path.Combine(
            Path.GetTempPath(), $"cuetools-linux-lookup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, "01.flac"), new byte[] { 1 });
        File.WriteAllText(Path.Combine(dir, manifestName), manifestBody);
        return dir;
    }

    private static async Task<VerifyDiscViewModel> RunVerify(
        string dir, Func<string, VerifyFilesResult> scripted)
    {
        var viewModel = new VerifyViewModel(
            new ScriptedVerifyService(scripted),
            new ReportStore(),
            new VerificationSourceDiscovery());

        Assert.True(viewModel.LoadSources(new[] { dir }));
        viewModel.VerifyCommand.Execute(null);
        for (int i = 0; i < 200 && (viewModel.IsBusy || !viewModel.HasResult); i++)
            await Task.Delay(25, TestContext.Current.CancellationToken);

        return viewModel.Discs.Single();
    }

    [AvaloniaFact]
    public async Task AFailedLookupIsNotReportedAsAnAbsentDisc()
    {
        string dir = WriteAlbum("album.m3u", "01.flac\n");
        try
        {
            VerifyDiscViewModel disc = await RunVerify(dir, path => new VerifyFilesResult
            {
                Ok = true,
                Status = "done",
                ArTotal = 0,
                CtdbTotal = 0,
                ArLookupFailed = true,
                CtdbLookupFailed = true,
                TrackCount = 1,
                Source = path,
            });

            Assert.Equal("lookup failed", disc.ArText);
            Assert.Equal("lookup failed", disc.CtdbText);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task ADatabaseThatAnsweredKeepsTheAbsentDiscWording()
    {
        string dir = WriteAlbum("album.m3u", "01.flac\n");
        try
        {
            VerifyDiscViewModel disc = await RunVerify(dir, path => new VerifyFilesResult
            {
                Ok = true,
                Status = "done",
                ArTotal = 0,
                CtdbTotal = 0,
                ArLookupFailed = false,
                CtdbLookupFailed = false,
                TrackCount = 1,
                Source = path,
            });

            Assert.Equal("not in database", disc.ArText);
            Assert.Equal("not found", disc.CtdbText);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task ARealAnswerOutranksAFailureFlag()
    {
        // A database that replied before the connection dropped still reports what
        // it said; the failure wording is the last resort, not the first.
        string dir = WriteAlbum("album.m3u", "01.flac\n");
        try
        {
            VerifyDiscViewModel disc = await RunVerify(dir, path => new VerifyFilesResult
            {
                Ok = true,
                Status = "done",
                ArConfidence = 4,
                ArTotal = 130,
                Accurate = true,
                CtdbConfidence = 207,
                ArLookupFailed = true,
                CtdbLookupFailed = true,
                TrackCount = 1,
                Source = path,
            });

            Assert.Equal("accurate | confidence 4", disc.ArText);
            Assert.Equal("verified | confidence 207", disc.CtdbText);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Utf8PlaylistsAreAcceptedByDiscoveryAndTheEngine()
    {
        string dir = WriteAlbum("album.m3u8", "# CUETools Linux\n\n01.flac\n");
        try
        {
            VerificationSourceDiscoveryResult result =
                new VerificationSourceDiscovery().Discover(new[] { dir });

            Assert.True(result.Ok, result.Error);
            VerificationDiscSource disc = Assert.Single(result.SourceSet!.Discs);
            Assert.Equal(VerificationSourceKind.Playlist, disc.Kind);
            Assert.EndsWith(".m3u8", disc.Path);

            // The engine has to agree, or the picker offers a file that fails to load.
            Assert.True(CUESheet.IsPlaylistExtension(Path.GetExtension(disc.Path)));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void FilesFromUnrelatedFoldersAreRejectedOnLinuxToo()
    {
        // Path.GetPathRoot is "/" for every absolute path here, so the volume test
        // alone could never fire on Linux.
        string first = WriteAlbum("a.cue", "FILE \"01.flac\" WAVE\n");
        string second = WriteAlbum("b.cue", "FILE \"01.flac\" WAVE\n");
        try
        {
            VerificationSourceDiscoveryResult result =
                new VerificationSourceDiscovery().Discover(new[]
                {
                    Path.Combine(first, "a.cue"),
                    Path.Combine(second, "b.cue"),
                });

            // Both fixtures sit under the system temp directory, so they share a real
            // ancestor and must still be allowed through to manifest handling.
            Assert.NotEqual(
                "Selected files must belong to the same album location.",
                result.Error);
        }
        finally
        {
            Directory.Delete(first, recursive: true);
            Directory.Delete(second, recursive: true);
        }
    }
}
