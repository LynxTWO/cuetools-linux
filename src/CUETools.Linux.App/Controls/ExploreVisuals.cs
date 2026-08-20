using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace CUETools.Linux.App.Controls;

/// <summary>
/// The Explore page's stage on this head: a to-scale 2D disc that pans and zooms
/// continuously from the whole disc down to the data spiral, standing in for the
/// WPF head's 3D orbit (this head reports HardwareAccelerated3D false).
///
/// Every dimension is the real CD spec: 120 mm disc, 15 mm center hole, clamping
/// area to 33 mm, program area 25-58 mm radius, track pitch 1.6 um, track width
/// ~0.5 um, and pit lengths of 3T to 11T at T = 0.278 um (the EFM run lengths).
/// The spiral and pits are drawn at their true geometry once the zoom resolves
/// them; only the run-length pattern is illustrative (a stable hash, not data
/// from any real disc), because the page teaches geometry, not content.
/// </summary>
public sealed class ExploreDisc : Control
{
    // radii in mm, from the Red Book physical spec
    private const double RDisc = 60.0, RHole = 7.5, RClamp = 16.5, RData0 = 25.0, RDataN = 58.0;
    private const double PitchMm = 0.0016;          // 1.6 um between spiral turns
    private const double TrackWidthMm = 0.0005;     // ~0.5 um pit width
    private const double TUnitMm = 0.000278;        // one EFM T at 1.2 m/s

    private double _zoom = -1;                      // px per mm; -1 = fit on first render
    private Point _centerMm;                        // world point (mm) at the viewport center
    private Point? _dragFrom;
    private Point _dragCenterStart;

    public ExploreDisc()
    {
        ClipToBounds = true;
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    private double FitZoom() => Math.Min(Bounds.Width, Bounds.Height) * 0.47 / RDisc;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _dragFrom = e.GetPosition(this);
        _dragCenterStart = _centerMm;
        e.Pointer.Capture(this);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _dragFrom = null;
        e.Pointer.Capture(null);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragFrom is not Point from || _zoom <= 0) return;
        Point now = e.GetPosition(this);
        _centerMm = new Point(
            Math.Clamp(_dragCenterStart.X - (now.X - from.X) / _zoom, -RDisc, RDisc),
            Math.Clamp(_dragCenterStart.Y - (now.Y - from.Y) / _zoom, -RDisc, RDisc));
        InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (_zoom <= 0) _zoom = FitZoom();
        double fit = FitZoom();
        Point p = e.GetPosition(this);
        // the world point under the cursor stays under the cursor
        Point worldAtCursor = ScreenToWorld(p);
        _zoom = Math.Clamp(_zoom * Math.Pow(1.3, e.Delta.Y), fit, 30000);
        Point back = ScreenToWorld(p);
        _centerMm = new Point(
            Math.Clamp(_centerMm.X + worldAtCursor.X - back.X, -RDisc, RDisc),
            Math.Clamp(_centerMm.Y + worldAtCursor.Y - back.Y, -RDisc, RDisc));
        InvalidateVisual();
        e.Handled = true;
    }

    private Point ScreenToWorld(Point s) => new(
        _centerMm.X + (s.X - Bounds.Width / 2) / _zoom,
        _centerMm.Y + (s.Y - Bounds.Height / 2) / _zoom);

    private Point WorldToScreen(Point w) => new(
        Bounds.Width / 2 + (w.X - _centerMm.X) * _zoom,
        Bounds.Height / 2 + (w.Y - _centerMm.Y) * _zoom);

    public override void Render(DrawingContext dc)
    {
        double w = Bounds.Width, h = Bounds.Height;
        if (w <= 0 || h <= 0) return;
        if (_zoom <= 0) _zoom = FitZoom();

        Color ground = GetColor("Ground", Color.FromRgb(0x0C, 0x0F, 0x0D));
        Color data = GetColor("DiscData", Color.FromRgb(0x93, 0xA3, 0x9F));
        Color hub = GetColor("DiscHub", Color.FromRgb(0xBC, 0xC8, 0xC4));
        Color edge = GetColor("DiscEdge", Color.FromRgb(0xDD, 0xE6, 0xE2));
        Color back = GetColor("DiscBack", Color.FromRgb(0x30, 0x3A, 0x36));
        Color teal = GetColor("StatusAccent", Color.FromRgb(0x34, 0xCF, 0xC0));
        Color mut = GetColor("Muted", Color.FromRgb(0x7C, 0x8A, 0x84));

        dc.FillRectangle(new SolidColorBrush(ground), new Rect(0, 0, w, h));

        Point c = WorldToScreen(new Point(0, 0));

        // The flat disc, painted as filled rings from the outside in. At deep zoom these
        // radii leave the viewport and the fills below cost nothing extra.
        Fill(dc, c, RDisc * _zoom, edge);                       // rim
        Fill(dc, c, (RDisc - 0.4) * _zoom, back);               // label-side body
        Fill(dc, c, RDataN * _zoom, data);                      // program area (metal)
        Fill(dc, c, RData0 * _zoom, back);                      // mirror band
        Fill(dc, c, RClamp * _zoom, hub);                       // clamping area
        Fill(dc, c, RHole * _zoom, ground);                     // center hole

        // Spiral turns resolve once their real 1.6 um pitch reaches ~1.5 px.
        double pitchPx = PitchMm * _zoom;
        if (pitchPx >= 1.5)
        {
            double alpha = Math.Clamp((pitchPx - 1.5) / 1.5, 0, 1);
            DrawSpiral(dc, ground, alpha);
        }

        ScaleBar(dc, w, h, teal, mut);
    }

    private static void Fill(DrawingContext dc, Point c, double rPx, Color color)
    {
        if (rPx <= 0) return;
        dc.DrawEllipse(new SolidColorBrush(color), null, c, rPx, rPx);
    }

    /// <summary>Draw the spiral turns crossing the viewport, and their pits once 3T
    /// (0.83 um, the shortest legal pit) spans a few pixels.</summary>
    private void DrawSpiral(DrawingContext dc, Color groove, double alpha)
    {
        // radial window of the viewport, clipped to the program area
        double half = Math.Max(Bounds.Width, Bounds.Height) / 2 / _zoom;
        double dist = Math.Sqrt(_centerMm.X * _centerMm.X + _centerMm.Y * _centerMm.Y);
        double rMin = Math.Max(RData0, dist - half), rMax = Math.Min(RDataN, dist + half);
        if (rMin >= rMax) return;

        int kFirst = (int)Math.Ceiling((rMin - RData0) / PitchMm);
        int kLast = (int)Math.Floor((rMax - RData0) / PitchMm);
        if (kLast - kFirst > 900) return;   // spacing under a pixel; the band fill is the display

        double grooveWidth = Math.Max(0.6, TrackWidthMm * _zoom * 0.6);
        var groovePen = new Pen(new SolidColorBrush(groove, 0.55 * alpha), grooveWidth);
        bool pits = TUnitMm * 3 * _zoom >= 5;
        var pitPen = pits
            ? new Pen(new SolidColorBrush(groove, 0.9 * alpha),
                Math.Max(1.2, TrackWidthMm * _zoom), lineCap: PenLineCap.Round)
            : null;

        // angular window: sample the arc only where the viewport is
        double aCenter = Math.Atan2(_centerMm.Y, _centerMm.X);
        for (int k = kFirst; k <= kLast; k++)
        {
            double r = RData0 + k * PitchMm;
            double aHalf = dist > 0.01
                ? Math.Min(Math.PI, 2 * Math.Asin(Math.Min(1, half / Math.Max(r, half))))
                : Math.PI;
            DrawTurnArc(dc, groovePen, r, aCenter - aHalf, aCenter + aHalf);
            if (pitPen != null)
                DrawPits(dc, pitPen, k, r, aCenter - aHalf, aCenter + aHalf);
        }
    }

    private void DrawTurnArc(DrawingContext dc, Pen pen, double rMm, double a0, double a1)
    {
        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            const int steps = 24;
            for (int i = 0; i <= steps; i++)
            {
                double a = a0 + (a1 - a0) * i / steps;
                Point p = WorldToScreen(new Point(rMm * Math.Cos(a), rMm * Math.Sin(a)));
                if (i == 0) ctx.BeginFigure(p, false);
                else ctx.LineTo(p);
            }
            ctx.EndFigure(false);
        }
        dc.DrawGeometry(null, pen, g);
    }

    /// <summary>Pits along one turn: EFM run lengths (3T..11T) from a stable hash of the
    /// turn and slot, so the pattern holds still while panning. Illustrative pattern,
    /// true geometry.</summary>
    private void DrawPits(DrawingContext dc, Pen pen, int turn, double rMm, double a0, double a1)
    {
        double arcLen = (a1 - a0) * rMm;
        int slots = (int)(arcLen / (TUnitMm * 14));
        if (slots < 1 || slots > 600) return;
        for (int s = 0; s < slots; s++)
        {
            uint hsh = Hash((uint)turn * 2654435761u + (uint)s * 40503u);
            double pitT = 3 + (hsh & 0xFF) % 9;              // 3T..11T pit
            double slotStart = a0 + (a1 - a0) * s / slots;
            double pitArc = pitT * TUnitMm / rMm;
            var g = new StreamGeometry();
            using (var ctx = g.Open())
            {
                Point p0 = WorldToScreen(new Point(rMm * Math.Cos(slotStart), rMm * Math.Sin(slotStart)));
                Point p1 = WorldToScreen(new Point(rMm * Math.Cos(slotStart + pitArc), rMm * Math.Sin(slotStart + pitArc)));
                ctx.BeginFigure(p0, false);
                ctx.LineTo(p1);
                ctx.EndFigure(false);
            }
            dc.DrawGeometry(null, pen, g);
        }
    }

    private static uint Hash(uint x)
    {
        x ^= x >> 16; x *= 0x7feb352d; x ^= x >> 15; x *= 0x846ca68b; x ^= x >> 16;
        return x;
    }

    /// <summary>A real scale bar: a round-number length that currently spans 60-160 px,
    /// so the zoom always has an honest ruler under it.</summary>
    private void ScaleBar(DrawingContext dc, double w, double h, Color teal, Color mut)
    {
        double[] steps = { 50, 20, 10, 5, 2, 1, 0.5, 0.2, 0.1, 0.05, 0.02, 0.01, 0.005, 0.002, 0.001, 0.0005 };
        foreach (double mm in steps)
        {
            double px = mm * _zoom;
            if (px is < 60 or > 160) continue;
            double x = 16, y = h - 22;
            // a quiet chip behind the ruler so it stays readable over the metal band
            dc.DrawRectangle(new SolidColorBrush(GetColor("Ground", Color.FromRgb(0x0C, 0x0F, 0x0D)), 0.72),
                null, new RoundedRect(new Rect(x - 8, y - 26, px + 16, 36), 6));
            var pen = new Pen(new SolidColorBrush(teal), 2);
            dc.DrawLine(pen, new Point(x, y), new Point(x + px, y));
            dc.DrawLine(pen, new Point(x, y - 4), new Point(x, y + 4));
            dc.DrawLine(pen, new Point(x + px, y - 4), new Point(x + px, y + 4));
            string label = mm >= 1 ? $"{mm:0.#} mm" : $"{mm * 1000:0.#} um";
            var ft = new FormattedText(label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("monospace"), 11, new SolidColorBrush(mut));
            dc.DrawText(ft, new Point(x, y - 20));
            break;
        }
    }

    private Color GetColor(string key, Color fallback)
        => this.TryFindResource(key, ActualThemeVariant, out object? value) &&
           value is ISolidColorBrush brush
            ? brush.Color
            : fallback;
}

/// <summary>
/// A cross-section of a CD (Avalonia port of the WPF control): the laser enters from the
/// clear reading side, passes through the 1.2 mm polycarbonate, focuses on the pits at the
/// thin aluminium reflective layer, and reflects back. This is the "why" behind CTDB repair
/// and the clear-side-vs-label-side asymmetry: a scratch on the clear side is out of focus
/// (often recoverable), a pin-hole or scratch on the label side destroys the reflective
/// layer itself (the data is gone). Dimensions are the real CD spec.
/// </summary>
public sealed class LayerCross : Control
{
    private static readonly Color Teal = Color.FromRgb(0x34, 0xCF, 0xC0);
    private static readonly Color Crit = Color.FromRgb(0xEF, 0x6D, 0x6D);
    private static readonly Color Good = Color.FromRgb(0x5C, 0xCB, 0x8B);

    public override void Render(DrawingContext dc)
    {
        double w = Bounds.Width, h = Bounds.Height;
        if (w <= 0 || h <= 0) return;
        Color ink = GetColor("InkDim", Color.FromRgb(0xD4, 0xDC, 0xD2));
        Color mut = GetColor("Muted", Color.FromRgb(0x7C, 0x8A, 0x84));

        double left = 4, right = w - 150, bandW = right - left;   // leave room for labels on the right
        double yLabel = 8, hLabel = 12;
        double yLacq = yLabel + hLabel, hLacq = 7;
        double yAlu = yLacq + hLacq, hAlu = 7;
        double yPoly = yAlu + hAlu, hPoly = h - yPoly - 34;       // the thick 1.2 mm substrate
        double yRead = yPoly + hPoly;

        Band(dc, left, yLabel, bandW, hLabel, Color.FromRgb(0x2A, 0x2E, 0x33), "label");
        Band(dc, left, yLacq, bandW, hLacq, Color.FromRgb(0x3A, 0x34, 0x22), "lacquer");
        Band(dc, left, yAlu, bandW, hAlu, Color.FromRgb(0xB8, 0xBE, 0xC6), null);
        var pit = new SolidColorBrush(Color.FromRgb(0x14, 0x18, 0x16));
        for (int i = 0; i < 9; i++)
        {
            double px = left + bandW * (0.12 + 0.09 * i) + (i % 2) * 3;
            dc.FillRectangle(pit, new Rect(px, yAlu + 1.5, 5 + (i % 3) * 3, hAlu - 3));
        }
        Band(dc, left, yPoly, bandW, hPoly, Color.FromArgb(0x30, 0x6C, 0xB6, 0xD6), null);

        // the laser: a focusing cone from the reading side up to a pit, then the return
        double cx = left + bandW * 0.5, focusY = yAlu + hAlu;
        var beam = new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0x6A, 0x5A));
        var cone = new StreamGeometry();
        using (var ctx = cone.Open())
        {
            ctx.BeginFigure(new Point(cx - 26, h - 2), true);
            ctx.LineTo(new Point(cx, focusY));
            ctx.LineTo(new Point(cx + 26, h - 2));
            ctx.EndFigure(true);
        }
        dc.DrawGeometry(beam, null, cone);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(0xFF, 0x5A, 0x4A)), 1.4),
            new Point(cx, h - 2), new Point(cx, focusY));

        Label(dc, right, yLabel + hLabel / 2, "label (printed)", ink, mut);
        Label(dc, right, yLacq + hLacq / 2, "lacquer", mut, mut);
        Label(dc, right, yAlu + hAlu / 2, "aluminium (data)", Teal, mut);
        Label(dc, right, yPoly + hPoly / 2, "polycarbonate 1.2mm", mut, mut);

        Note(dc, left, yLabel - 2, "label side: damage here is fatal", Crit);
        Note(dc, left, h - 2, "clear side: laser reads through (scratches recover)", Good);
    }

    private static void Band(DrawingContext dc, double x, double y, double w, double h, Color c, string? inlineLabel)
    {
        dc.FillRectangle(new SolidColorBrush(c), new Rect(x, y, w, h));
        if (inlineLabel != null && h >= 10)
        {
            var ft = Text(inlineLabel, 9, Color.FromArgb(0xC0, 0xE0, 0xE6, 0xDE));
            dc.DrawText(ft, new Point(x + 6, y + (h - ft.Height) / 2));
        }
    }

    private static void Label(DrawingContext dc, double x, double y, string s, Color c, Color leader)
    {
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(0x70, leader.R, leader.G, leader.B)), 1),
            new Point(x - 6, y), new Point(x + 6, y));
        var ft = Text(s, 10.5, c);
        dc.DrawText(ft, new Point(x + 10, y - ft.Height / 2));
    }

    private static void Note(DrawingContext dc, double x, double y, string s, Color c)
    {
        var ft = Text(s, 9.5, c);
        dc.DrawText(ft, new Point(x, y - ft.Height));
    }

    private static FormattedText Text(string s, double size, Color c) =>
        new(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("monospace"), size, new SolidColorBrush(c));

    private Color GetColor(string key, Color fallback)
        => this.TryFindResource(key, ActualThemeVariant, out object? value) &&
           value is ISolidColorBrush brush
            ? brush.Color
            : fallback;
}
