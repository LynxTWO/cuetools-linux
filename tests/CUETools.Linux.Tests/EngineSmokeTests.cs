using CUETools.AccurateRip;
using CUETools.CDImage;
using CUETools.Codecs;
using CUETools.CTDB;
using Xunit;

namespace CUETools.Linux.Tests;

// Engine-on-Linux smoke: pins the spike S-1/S-2 evidence (SPIKES-2026-08-11)
// as repeatable CI checks. These exercise the fork's engine through the
// pinned submodule, all-managed, no network.
public class EngineSmokeTests
{
    [Fact]
    public void WavRoundtripIsSampleExact()
    {
        var pcm = new AudioPCMConfig(16, 2, 44100);
        string path = Path.Combine(Path.GetTempPath(), $"cuetools-linux-smoke-{Guid.NewGuid():N}.wav");
        const int total = 44100 / 2;
        var samples = new int[total, 2];
        for (int i = 0; i < total; i++)
        {
            int v = (int)(Math.Sin(2 * Math.PI * 440 * i / 44100.0) * 20000);
            samples[i, 0] = v;
            samples[i, 1] = v;
        }

        try
        {
            var encoder = new CUETools.Codecs.WAV.AudioEncoder(
                new CUETools.Codecs.WAV.EncoderSettings(pcm), path);
            encoder.Write(new AudioBuffer(pcm, samples, total));
            encoder.Close();

            var decoder = new CUETools.Codecs.WAV.AudioDecoder(
                new CUETools.Codecs.WAV.DecoderSettings(), path);
            long got = 0;
            var buffer = new AudioBuffer(pcm, 4096);
            for (; ; )
            {
                int n = decoder.Read(buffer, -1);
                if (n <= 0)
                {
                    break;
                }

                for (int i = 0; i < n; i++)
                {
                    Assert.Equal(samples[got + i, 0], buffer.Samples[i, 0]);
                    Assert.Equal(samples[got + i, 1], buffer.Samples[i, 1]);
                }

                got += n;
            }

            decoder.Close();
            Assert.Equal(total, got);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void TocIdentitiesAreComputed()
    {
        var toc = CDImageLayout.FromString("150:20150:40150:60150");

        Assert.Equal(3, toc.TrackCount);
        Assert.False(string.IsNullOrEmpty(toc.TOCID));
        Assert.Equal("0001d718-0006205c-1c032003", AccurateRipVerify.CalculateAccurateRipId(toc));
        Assert.Equal("1C032003", AccurateRipVerify.CalculateCDDBId(toc));
    }

    [Fact]
    public void CtdbFingerprintIsStable()
    {
        string first = CUEToolsDB.GetUUID();
        string second = CUEToolsDB.GetUUID();

        // On the netstandard/net10 path this is a SHA-256 of the machine
        // name, base64url: 43 chars, no padding (spike S-2).
        Assert.Equal(first, second);
        Assert.Equal(43, first.Length);
        Assert.DoesNotContain('=', first);
    }
}
