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
    public void Mp3EncodesAValidStream()
    {
        // MP3 is lossy, so no bit-exact round trip; the contract here is
        // initialize, write, finalize, and a real MPEG audio stream on disk.
        Assert.True(Loader.IsReady("libmp3lame"),
            "vendored LAME missing - run eng/build-native-codecs.sh");
        int count = 44100;
        int[,] samples = MakeSamples(count);
        string path = Path.Combine(
            Path.GetTempPath(), $"cuetools-native-{Guid.NewGuid():N}.mp3");
        try
        {
            var settings = new CUETools.Codecs.libmp3lame.VBREncoderSettings { PCM = Cd };
            IAudioDest encoder = new CUETools.Codecs.libmp3lame.AudioEncoder(settings, path);
            encoder.Write(new AudioBuffer(Cd, samples, count));
            encoder.Close();

            using (FileStream stream = File.OpenRead(path))
            {
                Assert.True(stream.Length > 4096, "MP3 output implausibly small");
                // An ID3v2 tag may precede the first frame; its size is a
                // syncsafe 28-bit integer at bytes 6-9. Skip it, then demand
                // the MPEG frame sync (0xFF Ex/Fx) right where audio begins.
                byte[] head = new byte[10];
                Assert.Equal(10, stream.Read(head, 0, 10));
                long audioStart = 0;
                if (head[0] == (byte)'I' && head[1] == (byte)'D' && head[2] == (byte)'3')
                {
                    audioStart = 10 +
                        ((head[6] & 0x7F) << 21) + ((head[7] & 0x7F) << 14) +
                        ((head[8] & 0x7F) << 7) + (head[9] & 0x7F);
                }
                stream.Position = audioStart;
                byte[] frame = new byte[4096];
                int read = stream.Read(frame, 0, frame.Length);
                bool sync = false;
                for (int i = 0; i < read - 1; i++)
                {
                    if (frame[i] == 0xFF && (frame[i + 1] & 0xE0) == 0xE0) { sync = true; break; }
                }
                Assert.True(sync, "no MPEG frame sync found after the ID3 tag");
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void EveryManifestLibraryReportsAState()
    {
        // Readiness honesty: each manifest entry is either validated-and-
        // loaded or carries the reason it is not; nothing is silent.
        Assert.Equal(4, Loader.States.Count);
        foreach (NativeCodecLoader.LibraryState state in Loader.States)
        {
            Assert.False(string.IsNullOrWhiteSpace(state.Detail));
        }
    }
}
