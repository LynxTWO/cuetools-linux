using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace CUETools.Linux.App.Controls;

/// <summary>
/// Avalonia ports of the rip page's live visuals (fork Controls/, per the
/// disc-read-visualization skill): the 2D disc read map, the RMS VU pair,
/// the read-speed trace, and the re-read scope. Honesty rules carried over:
/// each control shows only its real event, every displayed count is the
/// literal ripper value, and only the animation is smoothed.
/// </summary>
public sealed class DiscReadMap : Control
{
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<DiscReadMap, double>(nameof(Progress));
    public static readonly StyledProperty<bool> SpinningProperty =
        AvaloniaProperty.Register<DiscReadMap, bool>(nameof(Spinning), true);

    public double Progress { get => GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }
    public bool Spinning { get => GetValue(SpinningProperty); set => SetValue(SpinningProperty, value); }

    // True CD radial proportions (mm), shared with the WPF disc model: the
    // equal-area mapping moves the pickup at constant data density (CLV).
    private const double RData0 = 25.0, RDataN = 58.0;
    internal static double DataRadius(double f) =>
        Math.Sqrt(RData0 * RData0 + Math.Clamp(f, 0, 1) * (RDataN * RDataN - RData0 * RData0));
    private static double SpinDegreesPerSecond(double f) => 145.0 * RData0 / DataRadius(f);

    private double _angle;
    private DateTime _last = DateTime.Now;
    private DispatcherTimer? _timer;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _last = DateTime.Now;
        _timer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(33), DispatcherPriority.Render, (_, _) =>
            {
                var now = DateTime.Now;
                double dt = Math.Min(0.05, (now - _last).TotalSeconds);
                _last = now;
                if (!Spinning) return;
                _angle = (_angle + dt * SpinDegreesPerSecond(Progress)) % 360.0;
                InvalidateVisual();
            });
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _timer?.Stop();
        _timer = null;
    }

    public override void Render(DrawingContext dc)
    {
        double w = Bounds.Width, h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var c = new Point(w / 2, h / 2);
        double radius = Math.Min(w, h) * 0.47;
        double holeRadius = radius * 7.5 / 60.0;
        double stackRadius = radius * 16.5 / 60.0;
        double dataInner = radius * RData0 / 60.0;
        double dataOuter = radius * RDataN / 60.0;

        Color ground = GetColor("Ground", Color.FromRgb(0x0C, 0x0F, 0x0D));
        Color data = GetColor("DiscData", Color.FromRgb(0x93, 0xA3, 0x9F));
        Color hub = GetColor("DiscHub", Color.FromRgb(0xBC, 0xC8, 0xC4));
        Color edge = GetColor("DiscEdge", Color.FromRgb(0xDD, 0xE6, 0xE2));
        Color back = GetColor("DiscBack", Color.FromRgb(0x30, 0x3A, 0x36));
        Color teal = GetColor("StatusAccent", Color.FromRgb(0x34, 0xCF, 0xC0));
        Color good = GetColor("StatusGood", Color.FromRgb(0x5C, 0xCB, 0x8B));

        var body = new RadialGradientBrush
        {
            GradientOrigin = new RelativePoint(0.38, 0.34, RelativeUnit.Relative),
            Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(hub, 0),
                new GradientStop(data, 0.48),
                new GradientStop(Lerp(data, back, 0.26), 0.78),
                new GradientStop(back, 1),
            },
        };
        dc.DrawEllipse(
            body,
            new Pen(new SolidColorBrush(edge), Math.Max(1, radius * 0.012)),
            c, radius, radius);

        // Representative spiral rings + narrow diffraction arcs, rotating with
        // the medium; never claimed to be the disc's literal EFM bits.
        var rotate = Matrix.CreateTranslation(-c.X, -c.Y) *
                     Matrix.CreateRotation(_angle * Math.PI / 180.0) *
                     Matrix.CreateTranslation(c.X, c.Y);
        using (dc.PushTransform(rotate))
        {
            var ringPen = new Pen(
                new SolidColorBrush(Color.FromArgb(34, 0xF0, 0xFA, 0xF6)),
                Math.Max(0.55, radius * 0.004));
            for (double r = dataInner; r < dataOuter; r += Math.Max(2.2, radius * 0.018))
                dc.DrawEllipse(null, ringPen, c, r, r);
            DrawArc(dc, c, radius * 0.77, -38, 84, Color.FromArgb(82, 0x5A, 0xE5, 0xD9), radius * 0.030);
            DrawArc(dc, c, radius * 0.82, -30, 70, Color.FromArgb(68, 0x8A, 0x86, 0xF2), radius * 0.025);
            DrawArc(dc, c, radius * 0.72, 142, 72, Color.FromArgb(60, 0xEE, 0xB4, 0x52), radius * 0.020);
        }

        // The ripped region grows inside-out to the real read fraction.
        double readR = radius * DataRadius(Progress) / 60.0;
        var ripped = new CombinedGeometry(
            GeometryCombineMode.Exclude,
            new EllipseGeometry(new Rect(c.X - readR, c.Y - readR, readR * 2, readR * 2)),
            new EllipseGeometry(new Rect(c.X - dataInner, c.Y - dataInner, dataInner * 2, dataInner * 2)));
        dc.DrawGeometry(
            new SolidColorBrush(Color.FromArgb(40, good.R, good.G, good.B)), null, ripped);
        dc.DrawEllipse(
            null,
            new Pen(new SolidColorBrush(Color.FromArgb(150, good.R, good.G, good.B)), 1.4),
            c, readR, readR);

        // Pickup lens at a fixed chassis angle while the medium rotates above it.
        const double pa = 0.5;
        var head = new Point(c.X + Math.Cos(pa) * readR, c.Y + Math.Sin(pa) * readR);
        var glow = new RadialGradientBrush
        {
            GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(190, teal.R, teal.G, teal.B), 0),
                new GradientStop(Color.FromArgb(0, teal.R, teal.G, teal.B), 1),
            },
        };
        dc.DrawEllipse(glow, null, head, 13, 13);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(0xFF, 0x6A, 0x58)), null, head, 2.4, 2.4);

        // Clear hub, stack ring, and centre hole retain the real proportions.
        var hubPen = new Pen(new SolidColorBrush(edge), Math.Max(1, radius * 0.008));
        dc.DrawEllipse(
            new RadialGradientBrush
            {
                GradientStops =
                {
                    new GradientStop(hub, 0),
                    new GradientStop(Color.FromArgb(150, hub.R, hub.G, hub.B), 1),
                },
            },
            hubPen, c, dataInner, dataInner);
        dc.DrawEllipse(null, hubPen, c, stackRadius, stackRadius);
        dc.DrawEllipse(new SolidColorBrush(ground), hubPen, c, holeRadius, holeRadius);
    }

    private static void DrawArc(
        DrawingContext dc, Point centre, double radius,
        double startDegrees, double sweepDegrees, Color color, double thickness)
    {
        double start = startDegrees * Math.PI / 180.0;
        double end = (startDegrees + sweepDegrees) * Math.PI / 180.0;
        var geometry = new StreamGeometry();
        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(
                new Point(centre.X + Math.Cos(start) * radius, centre.Y + Math.Sin(start) * radius),
                isFilled: false);
            context.ArcTo(
                new Point(centre.X + Math.Cos(end) * radius, centre.Y + Math.Sin(end) * radius),
                new Size(radius, radius),
                rotationAngle: 0,
                isLargeArc: Math.Abs(sweepDegrees) > 180,
                sweepDirection: sweepDegrees >= 0 ? SweepDirection.Clockwise : SweepDirection.CounterClockwise);
            context.EndFigure(false);
        }
        dc.DrawGeometry(
            null,
            new Pen(new SolidColorBrush(color), Math.Max(0.7, thickness))
            {
                LineCap = PenLineCap.Round,
            },
            geometry);
    }

    private Color GetColor(string key, Color fallback)
        => this.TryFindResource(key, ActualThemeVariant, out object? value) &&
           value is ISolidColorBrush brush
            ? brush.Color
            : fallback;

    internal static Color Lerp(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return Color.FromRgb(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t));
    }
}

/// <summary>
/// A pair of analog VU meters (L/R) driven by REAL per-channel RMS loudness.
/// RMS, not peak, with classic ballistics (fast attack, slow decay); the
/// needle holds its level during a read gap so the meter stays about the
/// music, distinct from the read-speed dip at the same moment.
/// </summary>
public sealed class VuMeter : Control
{
    public static readonly StyledProperty<bool> ActiveProperty =
        AvaloniaProperty.Register<VuMeter, bool>(nameof(Active));
    public static readonly StyledProperty<double> LevelLProperty =
        AvaloniaProperty.Register<VuMeter, double>(nameof(LevelL));
    public static readonly StyledProperty<double> LevelRProperty =
        AvaloniaProperty.Register<VuMeter, double>(nameof(LevelR));

    public bool Active { get => GetValue(ActiveProperty); set => SetValue(ActiveProperty, value); }
    public double LevelL { get => GetValue(LevelLProperty); set => SetValue(LevelLProperty, value); }
    public double LevelR { get => GetValue(LevelRProperty); set => SetValue(LevelRProperty, value); }

    private const double Attack = 0.35, Decay = 0.06;
    private double _l, _r;
    private DispatcherTimer? _timer;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _timer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(33), DispatcherPriority.Render, (_, _) =>
            {
                double tL = Active ? Deflection(LevelL) : 0;
                double tR = Active ? Deflection(LevelR) : 0;
                _l += (tL - _l) * (tL > _l ? Attack : Decay);
                _r += (tR - _r) * (tR > _r ? Attack : Decay);
                if (_l < 0.0005 && _r < 0.0005 && tL == 0 && tR == 0) { _l = _r = 0; return; }
                InvalidateVisual();
            });
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _timer?.Stop();
        _timer = null;
    }

    // Map 0..1 linear RMS onto needle deflection with a dB-like VU scale.
    private static double Deflection(double level)
    {
        if (level <= 0) return 0;
        double db = 20.0 * Math.Log10(Math.Min(1.0, level));
        return Math.Clamp((db + 40.0) / 40.0, 0, 1);
    }

    public override void Render(DrawingContext dc)
    {
        double w = Bounds.Width, h = Bounds.Height;
        if (w <= 0 || h <= 0) return;
        double radius = Math.Min(w * 0.2, h * 0.66);
        double cy = h * 0.86;
        Meter(dc, w * 0.27, cy, radius, "L", _l);
        Meter(dc, w * 0.73, cy, radius, "R", _r);
    }

    private void Meter(DrawingContext dc, double cx, double cy, double radius, string label, double val)
    {
        Color amber = GetColor("StatusWarning", Color.FromRgb(0xE9, 0xA6, 0x3F));
        Color muted = GetColor("Muted", Color.FromRgb(0x7D, 0x88, 0x7C));
        Color crit = GetColor("StatusDanger", Color.FromRgb(0xEF, 0x6D, 0x6D));
        const double a0 = -Math.PI * 0.75, a1 = -Math.PI * 0.25;

        var arc = new StreamGeometry();
        using (StreamGeometryContext ctx = arc.Open())
        {
            ctx.BeginFigure(Pt(cx, cy, radius, a0), isFilled: false);
            for (int i = 1; i <= 24; i++)
                ctx.LineTo(Pt(cx, cy, radius, a0 + (a1 - a0) * i / 24.0));
            ctx.EndFigure(false);
        }
        dc.DrawGeometry(
            null,
            new Pen(
                new SolidColorBrush(Color.FromArgb(40, amber.R, amber.G, amber.B)),
                Math.Max(2, radius * 0.16))
            { LineCap = PenLineCap.Round },
            arc);

        for (int i = 0; i <= 10; i++)
        {
            double a = a0 + (a1 - a0) * i / 10.0;
            bool over = i > 7;
            var pen = new Pen(
                new SolidColorBrush(over
                    ? crit
                    : Color.FromArgb(140, muted.R, muted.G, muted.B)),
                i % 5 == 0 ? 1.6 : 1);
            dc.DrawLine(pen, Pt(cx, cy, radius - radius * 0.06, a), Pt(cx, cy, radius + radius * 0.06, a));
        }

        double na = a0 + (a1 - a0) * Math.Clamp(val, 0, 1);
        var tip = Pt(cx, cy, radius + radius * 0.04, na);
        dc.DrawLine(
            new Pen(new SolidColorBrush(Color.FromArgb(90, amber.R, amber.G, amber.B)), 4.5)
            { LineCap = PenLineCap.Round },
            new Point(cx, cy), tip);
        dc.DrawLine(
            new Pen(new SolidColorBrush(amber), 1.8) { LineCap = PenLineCap.Round },
            new Point(cx, cy), tip);
        dc.DrawEllipse(new SolidColorBrush(amber), null, new Point(cx, cy), 2.6, 2.6);

        FontFamily family = this.TryFindResource("Mono", out object? value) && value is FontFamily mono
            ? mono
            : new FontFamily("monospace");
        var ft = new FormattedText(
            label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(family), 10,
            new SolidColorBrush(Color.FromArgb(220, muted.R, muted.G, muted.B)));
        dc.DrawText(ft, new Point(cx - ft.Width / 2, cy + radius * 0.05));
    }

    private Color GetColor(string key, Color fallback)
        => this.TryFindResource(key, ActualThemeVariant, out object? value) &&
           value is ISolidColorBrush brush
            ? brush.Color
            : fallback;

    private static Point Pt(double cx, double cy, double r, double a)
        => new(cx + Math.Cos(a) * r, cy + Math.Sin(a) * r);
}

/// <summary>
/// The live read-speed trace: a scrolling teal line + area fill over a faint
/// grid, newest sample on the right, sampled at a fixed cadence from the
/// view model's speed fraction.
/// </summary>
public sealed class SpeedGraph : Control
{
    public static readonly StyledProperty<double> LevelProperty =
        AvaloniaProperty.Register<SpeedGraph, double>(nameof(Level));
    public static readonly StyledProperty<bool> ActiveProperty =
        AvaloniaProperty.Register<SpeedGraph, bool>(nameof(Active));

    public double Level { get => GetValue(LevelProperty); set => SetValue(LevelProperty, value); }
    public bool Active { get => GetValue(ActiveProperty); set => SetValue(ActiveProperty, value); }

    private const int N = 140;
    private readonly double[] _buf = new double[N];
    private DispatcherTimer? _timer;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _timer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(140), DispatcherPriority.Render, (_, _) =>
            {
                Array.Copy(_buf, 1, _buf, 0, N - 1);
                double target = Active ? Math.Clamp(Level, 0, 1) : 0;
                _buf[N - 1] = _buf[N - 2] + (target - _buf[N - 2]) * 0.5;
                InvalidateVisual();
            });
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _timer?.Stop();
        _timer = null;
    }

    public override void Render(DrawingContext dc)
    {
        double w = Bounds.Width, h = Bounds.Height;
        if (w <= 0 || h <= 0) return;
        Color teal = GetColor("StatusAccent", Color.FromRgb(0x34, 0xCF, 0xC0));

        const double padT = 6, padB = 6;
        double gh = h - padT - padB;
        double Y(double v) => padT + gh * (1 - Math.Clamp(v, 0, 1));

        var grid = new Pen(new SolidColorBrush(Color.FromArgb(30, 210, 216, 200)), 1);
        for (int i = 1; i <= 3; i++)
            dc.DrawLine(grid, new Point(0, Y(i / 4.0)), new Point(w, Y(i / 4.0)));

        var line = new StreamGeometry();
        using (StreamGeometryContext ctx = line.Open())
        {
            ctx.BeginFigure(new Point(0, Y(_buf[0])), isFilled: false);
            for (int i = 1; i < N; i++)
                ctx.LineTo(new Point(w * i / (N - 1.0), Y(_buf[i])));
            ctx.EndFigure(false);
        }

        var area = new StreamGeometry();
        using (StreamGeometryContext ctx = area.Open())
        {
            ctx.BeginFigure(new Point(0, h - padB), isFilled: true);
            ctx.LineTo(new Point(0, Y(_buf[0])));
            for (int i = 1; i < N; i++)
                ctx.LineTo(new Point(w * i / (N - 1.0), Y(_buf[i])));
            ctx.LineTo(new Point(w, h - padB));
            ctx.EndFigure(true);
        }
        dc.DrawGeometry(
            new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0x66, teal.R, teal.G, teal.B), 0),
                    new GradientStop(Color.FromArgb(0x00, teal.R, teal.G, teal.B), 1),
                },
            },
            null, area);
        dc.DrawGeometry(
            null,
            new Pen(new SolidColorBrush(teal), 1.6) { LineJoin = PenLineJoin.Round },
            line);

        var end = new Point(w, Y(_buf[N - 1]));
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(0x55, teal.R, teal.G, teal.B)), null, end, 5, 5);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(0xEA, 0xFF, 0xFB)), null, end, 2, 2);
    }

    private Color GetColor(string key, Color fallback)
        => this.TryFindResource(key, ActualThemeVariant, out object? value) &&
           value is ISolidColorBrush brush
            ? brush.Color
            : fallback;
}

/// <summary>
/// A REAL sector re-read in progress, driven by the drive's own retry loop.
/// Count/Errors/Max are the literal ReadProgress values; the band is the
/// stuck window, the pits the disagreeing sectors, and the head sweeps
/// slower as the count climbs - the real drive drops its read speed on a
/// hard pass. Green the instant the sectors agree.
/// </summary>
public sealed class RereadScope : Control
{
    public static readonly StyledProperty<bool> ActiveProperty =
        AvaloniaProperty.Register<RereadScope, bool>(nameof(Active));
    public static readonly StyledProperty<int> CountProperty =
        AvaloniaProperty.Register<RereadScope, int>(nameof(Count));
    public static readonly StyledProperty<int> MaxProperty =
        AvaloniaProperty.Register<RereadScope, int>(nameof(Max), 30);
    public static readonly StyledProperty<int> ErrorsProperty =
        AvaloniaProperty.Register<RereadScope, int>(nameof(Errors));

    public bool Active { get => GetValue(ActiveProperty); set => SetValue(ActiveProperty, value); }
    public int Count { get => GetValue(CountProperty); set => SetValue(CountProperty, value); }
    public int Max { get => GetValue(MaxProperty); set => SetValue(MaxProperty, value); }
    public int Errors { get => GetValue(ErrorsProperty); set => SetValue(ErrorsProperty, value); }

    private double _alpha;
    private double _sweep;
    private DateTime _last = DateTime.Now;
    private DispatcherTimer? _timer;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _last = DateTime.Now;
        _timer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(33), DispatcherPriority.Render, (_, _) =>
            {
                var now = DateTime.Now;
                double dt = Math.Min(0.05, (now - _last).TotalSeconds);
                _last = now;
                double target = Active ? 1 : 0;
                _alpha += (target - _alpha) * (target > _alpha ? 0.25 : 0.06);
                double speed = 2.1 - 1.6 * Math.Min(1.0, Count / 8.0);
                _sweep += dt * Math.Max(0.4, speed);
                if (_sweep >= 1) _sweep -= 1;
                if (_alpha < 0.004 && !Active) { _alpha = 0; return; }
                InvalidateVisual();
            });
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _timer?.Stop();
        _timer = null;
    }

    public override void Render(DrawingContext dc)
    {
        double w = Bounds.Width, h = Bounds.Height;
        if (w <= 0 || h <= 0 || _alpha <= 0.004) return;

        Color amber = GetColor("StatusWarning", Color.FromRgb(0xE9, 0xA6, 0x3F));
        Color crit = GetColor("StatusDanger", Color.FromRgb(0xEF, 0x6D, 0x6D));
        Color good = GetColor("StatusGood", Color.FromRgb(0x5C, 0xCB, 0x8B));
        Color ink = GetColor("InkDim", Color.FromRgb(0xD4, 0xDC, 0xD2));

        bool converged = Errors <= 0 && Count > 0;
        double sev = Math.Min(1.0, Count / (double)Math.Max(1, Max));
        Color hot = converged ? good : DiscReadMap.Lerp(amber, crit, sev);
        byte a = (byte)(255 * Math.Min(1, _alpha));

        FontFamily family = this.TryFindResource("Mono", out object? value) && value is FontFamily mono
            ? mono
            : new FontFamily("monospace");
        var countFt = new FormattedText(
            "x" + Math.Max(1, Count), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(family, FontStyle.Normal, FontWeight.Bold), 22,
            new SolidColorBrush(Color.FromArgb(a, hot.R, hot.G, hot.B)));
        dc.DrawText(countFt, new Point(1, 0));

        string sub = converged ? "recovered"
            : Errors == 1 ? "1 sector disagrees"
            : Errors + " sectors disagree";
        dc.DrawText(
            new FormattedText(
                sub, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(family), 10.5,
                new SolidColorBrush(Color.FromArgb((byte)(a * 0.85), ink.R, ink.G, ink.B))),
            new Point(countFt.Width + 9, 8));

        double bandTop = h - 15, bandH = 9, bandL = 1, bandR = w - 1, bandW = bandR - bandL;
        var band = new Rect(bandL, bandTop, bandW, bandH);
        dc.DrawRectangle(
            new SolidColorBrush(Color.FromArgb((byte)(a * 0.18), hot.R, hot.G, hot.B)),
            new Pen(new SolidColorBrush(Color.FromArgb((byte)(a * 0.5), hot.R, hot.G, hot.B)), 1),
            new RoundedRect(band, 3));

        using (dc.PushClip(band))
        {
            int pits = Math.Min(Errors, 48);
            var pitBrush = new SolidColorBrush(Color.FromArgb((byte)(a * 0.92), 0x12, 0x16, 0x14));
            for (int i = 0; i < pits; i++)
            {
                double frac = Frac(i * 0.61803398875 + 0.13);
                double px = bandL + 3 + frac * (bandW - 6);
                dc.DrawRectangle(
                    pitBrush, null,
                    new RoundedRect(new Rect(px - 1.3, bandTop + 1.5, 2.6, bandH - 3), 1));
            }
            double hx = bandL + _sweep * bandW;
            dc.DrawRectangle(
                new SolidColorBrush(Color.FromArgb((byte)(a * 0.5), hot.R, hot.G, hot.B)),
                null, new Rect(hx - 4, bandTop, 8, bandH));
            dc.DrawLine(
                new Pen(new SolidColorBrush(Color.FromArgb(a, hot.R, hot.G, hot.B)), 1.6),
                new Point(hx, bandTop - 1.5), new Point(hx, bandTop + bandH + 1.5));
        }
    }

    private static double Frac(double x) => x - Math.Floor(x);

    private Color GetColor(string key, Color fallback)
        => this.TryFindResource(key, ActualThemeVariant, out object? value) &&
           value is ISolidColorBrush brush
            ? brush.Color
            : fallback;
}
