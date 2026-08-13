using CUETools.Codecs;
using CUETools.Linux.App;
using CUETools.Linux.App.Services;
using Xunit;

namespace CUETools.Linux.Tests;

// SLICE-005 native codec runtime (D-042): the vendored .so libraries are
// hash-validated, path-registered, and each codec proves the full contract
// the release evidence language demands - initialize, write, finalize, and
// read back - not merely a version getter. Tests fail loudly when the
// vendored natives are absent: eng/build-native-codecs.sh is part of the
// build for this slice onward.
public class NativeCodecTests
{
    private static readonly object Gate = new();
    private static NativeCodecLoader? _loader;

    private static NativeCodecLoader Loader
    {
        get
        {
            lock (Gate)
            {
                return _loader ??= Composition.RegisterNativeCodecs(_ => { });
            }
        }
    }

    private static readonly AudioPCMConfig Cd = new(16, 2, 44100);

    private static int[,] MakeSamples(int count)
    {
        var samples = new int[count, 2];
        for (int i = 0; i < count; i++)
        {
            samples[i, 0] = (int)(short)(10000 * Math.Sin(i * 0.03) + 3000 * Math.Sin(i * 0.31));
            samples[i, 1] = (int)(short)(9000 * Math.Sin(i * 0.028 + 1) + 2500 * Math.Sin(i * 0.4));
        }
        return samples;
    }

    private static void RoundTrip(
        Func<string, IAudioDest> createEncoder,
        IAudioDecoderSettings decoderSettings,
        string extension)
    {
        int count = 44100;
        int[,] samples = MakeSamples(count);
        string path = Path.Combine(
            Path.GetTempPath(), $"cuetools-native-{Guid.NewGuid():N}.{extension}");
        try
        {
            IAudioDest encoder = createEncoder(path);
            encoder.Write(new AudioBuffer(Cd, samples, count));
            encoder.Close();
            Assert.True(new FileInfo(path).Length > 0, "encoder produced an empty file");

            IAudioSource decoder = decoderSettings.Open(path);
            Assert.Equal(2, decoder.PCM.ChannelCount);
            var buffer = new AudioBuffer(decoder.PCM, count);
            var left = new int[count];
            var right = new int[count];
            int total = 0;
            while (total < count)
            {
                int got = decoder.Read(buffer, count - total);
                if (got <= 0) break;
                for (int i = 0; i < got; i++)
                {
                    left[total + i] = buffer.Samples[i, 0];
                    right[total + i] = buffer.Samples[i, 1];
                }
                total += got;
            }
            decoder.Close();

            Assert.Equal(count, total);
            for (int i = 0; i < count; i++)
            {
                if (samples[i, 0] != left[i] || samples[i, 1] != right[i])
                    Assert.Fail($"sample {i} differs after the {extension} round trip");
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void NativeFlacRoundTripsBitExact()
    {
        Assert.True(Loader.IsReady("libFLAC_dynamic"),
            "vendored libFLAC missing - run eng/build-native-codecs.sh");
        RoundTrip(
            path => new CUETools.Codecs.libFLAC.Encoder(
                new CUETools.Codecs.libFLAC.EncoderSettings { PCM = Cd }, path),
            new CUETools.Codecs.libFLAC.DecoderSettings(),
            "flac");
    }

    [Fact]
    public void WavPackRoundTripsBitExact()
    {
        Assert.True(Loader.IsReady("wavpackdll"),
            "vendored WavPack missing - run eng/build-native-codecs.sh");
        RoundTrip(
            path => new CUETools.Codecs.libwavpack.AudioEncoder(
                new CUETools.Codecs.libwavpack.EncoderSettings { PCM = Cd }, path),
            new CUETools.Codecs.libwavpack.DecoderSettings(),
            "wv");
    }

    [Fact]
    public void MonkeysAudioRoundTripsBitExact()
    {
        Assert.True(Loader.IsReady("MACLibDll"),
            "vendored Monkey's Audio missing - run eng/build-native-codecs.sh");
        RoundTrip(
            path => new CUETools.Codecs.MACLib.AudioEncoder(
                new CUETools.Codecs.MACLib.EncoderSettings { PCM = Cd }, path),
            new CUETools.Codecs.MACLib.DecoderSettings(),
            "ape");
    }

    [Fact]
    public void EveryManifestLibraryReportsAState()
    {
        // Readiness honesty: each manifest entry is either validated-and-
        // loaded or carries the reason it is not; nothing is silent.
        Assert.Equal(3, Loader.States.Count);
        foreach (NativeCodecLoader.LibraryState state in Loader.States)
        {
            Assert.False(string.IsNullOrWhiteSpace(state.Detail));
        }
    }
}
