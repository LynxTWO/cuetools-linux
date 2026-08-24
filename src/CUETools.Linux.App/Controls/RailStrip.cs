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

    private readonly Border _face;
    private readonly Border _recess;
    private readonly Border _dip;

    public RailStripKey(Geometry glyph, string pageName)
    {
        Width = 44;
        Height = 38;
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

        // SLICE-015 / D-080 (4): these keys deform like the rest. That needs the
        // same layers the key template builds in XAML, and this control draws
        // itself, so they are built here under the names the shared soft-body
        // renderer looks for. The glyph lives INSIDE the face so it shears with
        // the rubber rather than floating over it (D-080 (2)).
        //
        // No "keySeam" here on purpose. The rail key's housing lamp is composed
        // into its own BoxShadow in Restyle below, and naming a seam layer would
        // hand the same property to two writers - the renderer would clear it on
        // release and take the hover glow with it.
        _dip = new Border
        {
            Name = "keyDip",
            Opacity = 0,
            IsHitTestVisible = false,
            CornerRadius = new CornerRadius(7),
        };
        _face = new Border
        {
            Name = "keyFace",
            CornerRadius = new CornerRadius(7),
            BorderThickness = new Thickness(1),
            ClipToBounds = false,
            Child = new Panel { Children = { _dip, _glyph } },
        };
        _recess = new Border
        {
            Name = "keyRecess",
            Opacity = 0,
            IsHitTestVisible = false,
            CornerRadius = new CornerRadius(7),
        };

        Background = Brushes.Transparent;
        BorderThickness = new Thickness(0);
        Child = new Panel { ClipToBounds = false, Children = { _recess, _face } };
    }

    public bool IsActiveKey { get; private set; }

    /// <summary>Pointer state for the housing lamp (D-088). These keys draw
    /// themselves rather than living in a template, so the seam light that the
    /// key styles get from XAML has to be built here instead. D-080 (4) put the
    /// rail in scope precisely so the most-clicked surface in the app would not
    /// be the one thing that stopped responding.</summary>
    private bool _hot;
    private bool _held;

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
        _face.Background = this.TryFindResource("ButtonFace", ActualThemeVariant, out object? face) && face is IBrush b
            ? b
            : Brushes.Transparent;
        _face.BorderBrush = this.TryFindResource("ButtonEdge", ActualThemeVariant, out object? edge) && edge is IBrush eb
            ? eb
            : Brushes.Transparent;
        // the housing wall a receding face reveals; without it the gap shows the page
        _recess.Background = this.TryFindResource("ButtonEdge", ActualThemeVariant, out object? wall) && wall is IBrush wb
            ? wb
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
            _face.BoxShadow = BoxShadows.Parse(dark ? "0 0 10 0 #7034CFC0" : "0 0 10 0 #50087067");
        }
        else
        {
            _glyph.Stroke = new SolidColorBrush(Color.Parse(dark ? "#4A554B" : "#8A968B"));
            _glyph.Effect = null;
            _face.BoxShadow = BoxShadows.Parse(dark ? "0 1.5 3 0 #70000000" : "0 1.5 3 0 #30536057");
        }

        // the housing lamp under the key, over whatever shadow the state above
        // chose. An active key keeps its teal halo and gains the warm one, the
        // same way a lit key on a console still lights its own seam.
        if (_hot || _held)
        {
            string seam = this.TryFindResource("KeySeamColor", ActualThemeVariant, out object? c) && c is Color sc
                ? $"{sc.R:X2}{sc.G:X2}{sc.B:X2}"
                : (dark ? "F0A24A" : "C9762A");
            string alpha = _held ? "B0" : "55";
            _face.BoxShadow = BoxShadows.Parse($"{_face.BoxShadow}, 0 1 13 0 #{alpha}{seam}");
        }
    }

    protected override void OnPointerEntered(Avalonia.Input.PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        _hot = true;
        Restyle();
    }

    protected override void OnPointerExited(Avalonia.Input.PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hot = false;
        _held = false;
        Restyle();
    }

    protected override void OnPointerPressed(Avalonia.Input.PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _held = true;
        Restyle();
    }

    protected override void OnPointerReleased(Avalonia.Input.PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _held = false;
        Restyle();
    }
}
