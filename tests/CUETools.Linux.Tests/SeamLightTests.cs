using System.Text.RegularExpressions;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using CUETools.Linux.App.Controls;
using Xunit;

namespace CUETools.Linux.Tests;

// D-088. Hover and press are told by light from the housing rather than by an
// outline. Three things about that can break quietly, so all three are pinned.
public class SeamLightTests
{
    private static string Theme() => File.ReadAllText(
        Path.Combine(FindRepoRoot(), "src", "CUETools.Linux.App", "Theme", "AnalogControls.axaml"));

    private static string FindRepoRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "eng", "build.sh")))
            dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);
        return dir!;
    }

    private static double SeamOpacityFor(string pseudoClass)
    {
        // the style block for keySeam under this pseudo-class, and the opacity it sets
        var m = Regex.Match(
            Theme(),
            @"Selector=""Button:" + Regex.Escape(pseudoClass) +
            @"\s*/template/\s*Border#keySeam""\s*>\s*<Setter\s+Property=""Opacity""\s+Value=""([0-9.]+)""",
            RegexOptions.Singleline);
        Assert.True(m.Success, $"no keySeam opacity setter found for :{pseudoClass}");
        return double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    [Fact]
    public void ThePressRampStartsExactlyWhereHoverLeftOff()
    {
        // the styles own the resting and hover levels; the soft-body ramp drives
        // the same property while a press is held and hands it back on release.
        // If the two disagree the lamp jumps the instant the key is touched, and
        // nothing else in the suite would notice.
        Assert.Equal(SoftBodyKey.SeamHover, SeamOpacityFor("pointerover"), 3);
        Assert.Equal(SoftBodyKey.SeamPressed, SeamOpacityFor("pressed"), 3);
        Assert.True(SoftBodyKey.SeamPressed > SoftBodyKey.SeamHover,
            "pressing a key has to brighten its lamp, not dim it");
    }

    [Fact]
    public void HoverNoLongerDrawsAnOutlineOnTheKeyFace()
    {
        // the owner's report (2026-08-23): the hover outline popped up and then
        // persisted through the press. An outline is a UI convention wearing a
        // console's clothes; this guards the replacement from being undone.
        Assert.DoesNotMatch(
            new Regex(@"Selector=""Button:pointerover\s*/template/\s*Border#keyFace"""),
            Theme());
    }

    [AvaloniaFact]
    public void TheHousingLampIsWarmAndNotMistakableForTheAccent()
    {
        // the console is cool green-grey with a teal accent. The lamp has to read
        // as light from inside the box, which means warm - and a lamp that drifted
        // toward the accent hue would read as another status colour instead.
        foreach (ThemeVariant variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            Assert.True(Avalonia.Application.Current!.TryGetResource(
                "KeySeamColor", variant, out object? value), "KeySeamColor missing");
            var seam = Assert.IsType<Color>(value);

            Assert.True(seam.R > seam.B + 60,
                $"{variant} KeySeamColor #{seam} is not warm: red must lead blue clearly");
            Assert.True(seam.R >= seam.G && seam.G > seam.B,
                $"{variant} KeySeamColor #{seam} is not on the amber side of warm");

            Assert.True(Avalonia.Application.Current.TryGetResource(
                "StatusAccentColor", variant, out object? accentValue), "StatusAccentColor missing");
            var accent = Assert.IsType<Color>(accentValue);
            int distance = Math.Abs(seam.R - accent.R) + Math.Abs(seam.G - accent.G) + Math.Abs(seam.B - accent.B);
            Assert.True(distance > 150,
                $"{variant} the housing lamp #{seam} is too close to the accent #{accent} to read as a different fixture");
        }
    }
}
