using Avalonia.Media;

namespace CUETools.Linux.App.Theme;

/// <summary>
/// The rail strip's icon set (SLICE-013 build item one, D-075/D-076): one
/// etched glyph per page, drawn as strokes so a backlit key can light the
/// groove. Every glyph is a metaphor from the bench, not an abstract app
/// icon: Settings is a bank of mixer faders, Advanced is the recessed
/// trimmer screw you only touch deliberately, How a CD Works is the
/// magnifier over the disc. The path data itself lives in App.Core
/// (RailIconPaths, keyed by the shared page titles) so both heads parse one
/// source; this file binds it to Avalonia geometry and the rail order.
///
/// The lit treatment (owner-corrected on the approval sheet): the groove's
/// light needs real falloff, not a wider band. Render the lit stroke with a
/// zero-offset DropShadowEffect in the halo color (blur ~8 dark / ~7 light,
/// opacity ~0.9 / ~0.65), the same physics as the key's own halo.
/// </summary>
public static class RailIcons
{
    /// <summary>A disc with its audio drawn out: the extraction itself.</summary>
    public static readonly Geometry Rip = Geometry.Parse(
        CUETools.Wpf.Theme.RailIconPaths.ForTitle("Rip")!);

    /// <summary>The verification check, bold and alone.</summary>
    public static readonly Geometry Verify = Geometry.Parse(
        CUETools.Wpf.Theme.RailIconPaths.ForTitle("Verify & Repair")!);

    /// <summary>Two opposing arrows: one format in, another out.</summary>
    public static readonly Geometry Convert = Geometry.Parse(
        CUETools.Wpf.Theme.RailIconPaths.ForTitle("Convert")!);

    /// <summary>Stacked jobs, the bottom one moving.</summary>
    public static readonly Geometry Queue = Geometry.Parse(
        CUETools.Wpf.Theme.RailIconPaths.ForTitle("Queue")!);

    /// <summary>The certificate: a document carrying its seal.</summary>
    public static readonly Geometry Report = Geometry.Parse(
        CUETools.Wpf.Theme.RailIconPaths.ForTitle("Report")!);

    /// <summary>The template's own percent sign.</summary>
    public static readonly Geometry Naming = Geometry.Parse(
        CUETools.Wpf.Theme.RailIconPaths.ForTitle("Naming")!);

    /// <summary>The drive: a tray with its slot and activity light.</summary>
    public static readonly Geometry Drive = Geometry.Parse(
        CUETools.Wpf.Theme.RailIconPaths.ForTitle("Drive & Read")!);

    /// <summary>A bank of mixer faders, each ridden to its own level.</summary>
    public static readonly Geometry Settings = Geometry.Parse(
        CUETools.Wpf.Theme.RailIconPaths.ForTitle("Settings")!);

    /// <summary>A trim knob with its pointer: adjusted deliberately, rarely.</summary>
    public static readonly Geometry Advanced = Geometry.Parse(
        CUETools.Wpf.Theme.RailIconPaths.ForTitle("Advanced")!);

    /// <summary>The magnifier over the disc: zoom until the spiral resolves.</summary>
    public static readonly Geometry Explore = Geometry.Parse(
        CUETools.Wpf.Theme.RailIconPaths.ForTitle("How a CD Works")!);

    /// <summary>The rail's order, matching the nav cards top to bottom.</summary>
    public static readonly (string Name, Geometry Glyph)[] All =
    {
        ("Verify & Repair", Verify),
        ("Convert", Convert),
        ("Queue", Queue),
        ("Rip", Rip),
        ("Settings", Settings),
        ("Report", Report),
        ("Naming", Naming),
        ("Drive & Read", Drive),
        ("Advanced", Advanced),
        ("How a CD Works", Explore),
    };
}
