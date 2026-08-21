using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using CUETools.Linux.App.Theme;

namespace CUETools.Linux.App.Controls;

/// <summary>Which rail the window earns at its current width (SLICE-013, D-076):
/// the full card rail at 1140 logical pixels and up, the icon strip below it,
/// and the floor (horizontal scrolling instead of clipping) below 860.</summary>
public enum RailLayout
{
    Full,
    Compact,
    Floor,
}

public static class RailBreakpoints
{
    // the numbers live in App.Core (CUETools.Wpf.Theme.RailBreakpointValues)
    // so both heads read one source; this head adds only the state mapping
    public const double FullAt = CUETools.Wpf.Theme.RailBreakpointValues.FullAt;
    public const double FloorBelow = CUETools.Wpf.Theme.RailBreakpointValues.FloorBelow;

    public static RailLayout For(double windowWidth) =>
        windowWidth >= FullAt ? RailLayout.Full
        : windowWidth >= FloorBelow ? RailLayout.Compact
        : RailLayout.Floor;
}

/// <summary>
/// One key of the collapsed rail's icon strip: the owner-approved sheet visual
/// (docs/evidence/2026-08-20-slice013-icon-sheet-*.png). A machined key face
/// carrying an etched glyph; the active page's key lights its groove with real
/// falloff (a zero-offset shadow blur of the stroke, the key halo's physics).
/// The colors are the approved sheet's own values, resolved per theme in
/// Restyle - the same call-time pattern the nav cards use, for the same
/// reason (a theme flip must restyle, not strand old brushes).
/// </summary>
public sealed class RailStripKey : Border
{
    private readonly Avalonia.Controls.Shapes.Path _glyph;

    public RailStripKey(Geometry glyph, string pageName)
    {
        Width = 44;
        Height = 38;
        CornerRadius = new CornerRadius(7);
        BorderThickness = new Thickness(1);
        ClipToBounds = false;
        Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
        ToolTip.SetTip(this, pageName);
        _glyph = new Avalonia.Controls.Shapes.Path
        {
            Data = glyph,
            StrokeThickness = 2,
            StrokeLineCap = PenLineCap.Round,
            StrokeJoin = PenLineJoin.Round,
            Stretch = Stretch.None,
            Width = 24,
            Height = 24,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        };
        Child = _glyph;
    }

    public bool IsActiveKey { get; private set; }

    public void SetActive(bool active)
    {
        IsActiveKey = active;
        Restyle();
    }

    /// <summary>Resolve the key's look for the current theme and state. Called on
    /// activation changes and on every theme flip (RestyleNavs' sibling).</summary>
    public void Restyle()
    {
        bool dark = ActualThemeVariant != Avalonia.Styling.ThemeVariant.Light;
        Background = this.TryFindResource("ButtonFace", ActualThemeVariant, out object? face) && face is IBrush b
            ? b
            : Brushes.Transparent;
        BorderBrush = this.TryFindResource("ButtonEdge", ActualThemeVariant, out object? edge) && edge is IBrush eb
            ? eb
            : Brushes.Transparent;
        if (IsActiveKey)
        {
            _glyph.Stroke = new SolidColorBrush(Color.Parse(dark ? "#C9FBF4" : "#0A8A7F"));
            _glyph.Effect = new DropShadowEffect
            {
                OffsetX = 0,
                OffsetY = 0,
                BlurRadius = dark ? 8 : 7,
                Color = Color.Parse(dark ? "#34CFC0" : "#087067"),
                Opacity = dark ? 0.9 : 0.65,
            };
            BoxShadow = BoxShadows.Parse(dark ? "0 0 10 0 #7034CFC0" : "0 0 10 0 #50087067");
        }
        else
        {
            _glyph.Stroke = new SolidColorBrush(Color.Parse(dark ? "#4A554B" : "#8A968B"));
            _glyph.Effect = null;
            BoxShadow = BoxShadows.Parse(dark ? "0 1.5 3 0 #70000000" : "0 1.5 3 0 #30536057");
        }
    }
}
