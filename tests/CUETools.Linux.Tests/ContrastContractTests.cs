using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace CUETools.Linux.Tests;

// WCAG AA, pinned: every text-role brush must reach 4.5:1 over every surface
// it can sit on, in both themes. This is the accessibility audit as a contract,
// so a palette edit that quietly washes out the light theme fails the build
// instead of shipping. Decorative constants (Teal, Good, Amber) are exactly
// that - decoration - and no view may use them as text Foreground; the sweep
// test below keeps them out.
public class ContrastContractTests
{
    private static readonly string[] TextRoles =
        { "Ink", "InkDim", "Muted", "StatusAccent", "StatusGood", "StatusWarning", "StatusDanger" };
    private static readonly string[] Surfaces = { "Ground", "Bar", "Face", "Panel", "Glass" };

    // The key faces a label actually sits on. ButtonFace is a gradient, so both of its
    // stops count as surfaces: a label must clear AA against the lightest AND darkest
    // band of the face beneath it. These were never pinned before SLICE-015's research
    // pointed out that the plain key face had no contrast contract at all.
    private static readonly string[] KeySurfaces = { "ButtonFace", "ButtonPressed" };

    private static double Luminance(Color c)
    {
        static double F(double v) => v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        double r = F(c.R / 255.0), g = F(c.G / 255.0), b = F(c.B / 255.0);
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    private static double Ratio(Color fg, Color bg)
    {
        double a = Luminance(fg), b = Luminance(bg);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    private static Color Resolve(string key, ThemeVariant variant)
    {
        Assert.True(Avalonia.Application.Current!.TryGetResource(key, variant, out object? value),
            $"palette key {key} missing");
        return value switch
        {
            ISolidColorBrush brush => brush.Color,
            Color color => color,
            _ => throw new Xunit.Sdk.XunitException($"{key} is neither a solid brush nor a color"),
        };
    }

    [AvaloniaFact]
    public void EveryTextRoleReadsAtWcagAaOverEverySurfaceInBothThemes()
    {
        foreach (ThemeVariant variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        foreach (string role in TextRoles)
        foreach (string surface in Surfaces)
        {
            double r = Ratio(Resolve(role, variant), Resolve(surface, variant));
            Assert.True(r >= 4.5,
                $"{variant} {role} on {surface}: {r:0.00}:1 is under WCAG AA 4.5:1");
        }
    }

    /// <summary>Every color a surface key can resolve to: one for a solid brush, one per
    /// stop for a gradient (a label sits on the whole span, not on an average).</summary>
    private static IEnumerable<Color> SurfaceColors(string key, ThemeVariant variant)
    {
        Assert.True(Avalonia.Application.Current!.TryGetResource(key, variant, out object? value),
            $"palette key {key} missing");
        switch (value)
        {
            case ISolidColorBrush solid:
                yield return solid.Color;
                break;
            case IGradientBrush gradient:
                Assert.NotEmpty(gradient.GradientStops);
                foreach (IGradientStop stop in gradient.GradientStops)
                    yield return stop.Color;
                break;
            default:
                throw new Xunit.Sdk.XunitException($"{key} is neither a solid brush nor a gradient");
        }
    }

    [AvaloniaFact]
    public void TheKeyFacesCarryTheirLabelsAtWcagAa()
    {
        // Ink is the label on a plain machined key, in both its resting and pressed
        // faces. Disabled labels are exempt (WCAG 1.4.3 covers active controls only).
        foreach (ThemeVariant variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        foreach (string surface in KeySurfaces)
        foreach (Color face in SurfaceColors(surface, variant))
        {
            double r = Ratio(Resolve("Ink", variant), face);
            Assert.True(r >= 4.5,
                $"{variant} Ink on {surface} stop #{face}: {r:0.00}:1 is under WCAG AA 4.5:1");
        }
    }

    [AvaloniaFact]
    public void TheAccentKeysLabelReadsAtWcagAaOverBothGradientStops()
    {
        // SLICE-014's accent key face is a gradient; its label must clear AA
        // against BOTH stops in both themes. Disabled keys are exempt by
        // WCAG 1.4.3 (inactive controls); every enabled state is covered here.
        foreach (ThemeVariant variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        foreach (string stop in new[] { "AccentKeyTop", "AccentKeyBottom" })
        {
            double r = Ratio(Resolve("AccentKeyText", variant), Resolve(stop, variant));
            Assert.True(r >= 4.5,
                $"{variant} AccentKeyText on {stop}: {r:0.00}:1 is under WCAG AA 4.5:1");
        }
    }

    [AvaloniaFact]
    public void NoViewUsesADecorativeConstantAsTextForeground()
    {
        // the decorative accents are variant-invariant and fail AA on the light
        // surfaces (measured 1.6:1); text must ride the Status* brushes instead
        string root = FindRepoRoot();
        var offenders = new List<string>();
        foreach (string file in Directory.GetFiles(
            Path.Combine(root, "src", "CUETools.Linux.App"), "*.axaml", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            foreach (string bad in new[]
            {
                "Foreground=\"{StaticResource Teal}\"",
                "Foreground=\"{StaticResource Teal2}\"",
                "Foreground=\"{StaticResource Good}\"",
                "Foreground=\"{StaticResource Amber}\"",
                "Property=\"Foreground\" Value=\"{StaticResource Teal}\"",
                "Property=\"Foreground\" Value=\"{StaticResource Good}\"",
                "Property=\"Foreground\" Value=\"{StaticResource Amber}\"",
            })
                if (text.Contains(bad))
                    offenders.Add($"{Path.GetFileName(file)}: {bad}");
        }
        Assert.Empty(offenders);
    }

    private static string FindRepoRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "eng", "build.sh")))
            dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);
        return dir!;
    }
}
