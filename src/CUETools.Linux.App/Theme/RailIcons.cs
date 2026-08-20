using Avalonia.Media;

namespace CUETools.Linux.App.Theme;

/// <summary>
/// The rail strip's icon set (SLICE-013 build item one, D-075/D-076): one
/// etched glyph per page, drawn as strokes so a backlit key can light the
/// groove. Every glyph is a metaphor from the bench, not an abstract app
/// icon: Settings is a bank of mixer faders, Advanced is the recessed
/// trimmer screw you only touch deliberately, How a CD Works is the
/// magnifier over the disc. 24x24 viewbox, 2px strokes, round caps.
/// </summary>
public static class RailIcons
{
    /// <summary>A disc with its audio drawn out: the extraction itself.</summary>
    public static readonly Geometry Rip = Geometry.Parse(
        "M 9.5,5 A 7,7 0 1 0 9.5,19 A 7,7 0 1 0 9.5,5 Z " +
        "M 9.5,10.5 A 1.5,1.5 0 1 0 9.5,13.5 A 1.5,1.5 0 1 0 9.5,10.5 Z " +
        "M 17,12 L 21.5,12 M 21.5,12 L 19.3,9.8 M 21.5,12 L 19.3,14.2");

    /// <summary>The verification check, bold and alone.</summary>
    public static readonly Geometry Verify = Geometry.Parse(
        "M 5,13 L 10,18 L 19,7");

    /// <summary>Two opposing arrows: one format in, another out.</summary>
    public static readonly Geometry Convert = Geometry.Parse(
        "M 6,9 L 15,9 M 15,9 L 12,6 M 15,9 L 12,12 " +
        "M 18,15 L 9,15 M 9,15 L 12,12.5 M 9,15 L 12,18");

    /// <summary>Stacked jobs, the bottom one moving.</summary>
    public static readonly Geometry Queue = Geometry.Parse(
        "M 5,7 L 19,7 M 5,12 L 19,12 M 5,17 L 12,17 " +
        "M 15,17 L 19,17 M 19,17 L 16.5,14.8 M 19,17 L 16.5,19.2");

    /// <summary>The certificate: a document carrying its seal.</summary>
    public static readonly Geometry Report = Geometry.Parse(
        "M 7,4 L 15,4 L 18,7 L 18,20 L 7,20 Z M 15,4 L 15,7 L 18,7 " +
        "M 14.5,14.5 A 2,2 0 1 0 14.5,18.5 A 2,2 0 1 0 14.5,14.5 Z");

    /// <summary>The template's own percent sign.</summary>
    public static readonly Geometry Naming = Geometry.Parse(
        "M 8.5,6 A 2.5,2.5 0 1 0 8.5,11 A 2.5,2.5 0 1 0 8.5,6 Z " +
        "M 15.5,13 A 2.5,2.5 0 1 0 15.5,18 A 2.5,2.5 0 1 0 15.5,13 Z " +
        "M 17,6 L 7,18");

    /// <summary>The drive: a tray with its slot and activity light.</summary>
    public static readonly Geometry Drive = Geometry.Parse(
        "M 6,8 L 18,8 A 2,2 0 0 1 20,10 L 20,15 A 2,2 0 0 1 18,17 " +
        "L 6,17 A 2,2 0 0 1 4,15 L 4,10 A 2,2 0 0 1 6,8 Z " +
        "M 7,11.5 L 17,11.5 M 16.6,14.6 L 16.8,14.6");

    /// <summary>A bank of mixer faders, each ridden to its own level.</summary>
    public static readonly Geometry Settings = Geometry.Parse(
        "M 7,5 L 7,19 M 12,5 L 12,19 M 17,5 L 17,19 " +
        "M 5.2,14.5 L 8.8,14.5 M 10.2,8.5 L 13.8,8.5 M 15.2,12 L 18.8,12");

    /// <summary>A trim knob with its pointer: adjusted deliberately, rarely.</summary>
    public static readonly Geometry Advanced = Geometry.Parse(
        "M 12,5 A 7,7 0 1 0 12,19 A 7,7 0 1 0 12,5 Z " +
        "M 12,12 L 16,8 M 12,12 L 12,12.01");

    /// <summary>The magnifier over the disc: zoom until the spiral resolves.</summary>
    public static readonly Geometry Explore = Geometry.Parse(
        "M 10.5,5 A 5.5,5.5 0 1 0 10.5,16 A 5.5,5.5 0 1 0 10.5,5 Z " +
        "M 10.5,9.2 A 1.3,1.3 0 1 0 10.5,11.8 A 1.3,1.3 0 1 0 10.5,9.2 Z " +
        "M 14.5,14.5 L 19,19");

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
