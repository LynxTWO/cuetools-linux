using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CUETools.Wpf.Controls;
using Pred = CUETools.Wpf.Controls.CodecMath.Pred;

namespace CUETools.Linux.App.Controls;

/// <summary>
/// Avalonia port of the WPF ConvertScope: the convert round trip
/// (source format -> PCM -> target format). The real reconstructed PCM in
/// the middle, and on each side the codec's real compactness (bits/sample
/// and packed bars) computed from that PCM by the shared CodecMath
/// predictors - the ratios are computed, not decorative. Fed real decoded
/// source samples via <see cref="Samples"/> while a convert runs; a gentle
/// demo keeps the round trip legible when idle.
/// </summary>
public sealed class ConvertScope : Control
{
    public static readonly StyledProperty<string> SourceCodecProperty =
        AvaloniaProperty.Register<ConvertScope, string>(nameof(SourceCodec), "flac");
    public static readonly StyledProperty<string> TargetCodecProperty =
        AvaloniaProperty.Register<ConvertScope, string>(nameof(TargetCodec), "wav");
    public static readonly StyledProperty<bool> ActiveProperty =
        AvaloniaProperty.Register<ConvertScope, bool>(nameof(Active));
    public static readonly StyledProperty<float[]?> SamplesProperty =
        AvaloniaProperty.Register<ConvertScope, float[]?>(nameof(Samples));

    public string SourceCodec { get => GetValue(SourceCodecProperty); set => SetValue(SourceCodecProperty, value); }
    public string TargetCodec { get => GetValue(TargetCodecProperty); set => SetValue(TargetCodecProperty, value); }
    public bool Active { get => GetValue(ActiveProperty); set => SetValue(ActiveProperty, value); }
    public float[]? Samples { get => GetValue(SamplesProperty); set => SetValue(SamplesProperty, value); }

    static ConvertScope()
    {
        SamplesProperty.Changed.AddClassHandler<ConvertScope>((scope, args) =>
        {
            if (args.NewValue is float[] { Length: > 0 } win) scope.Push(win);
        });
    }

    private const int Roll = 640;
    private readonly float[] _roll = new float[Roll];
    private readonly float[] _demo = new float[Roll];
    private float[] _show;                       // _roll when real audio flows, else the idle demo
    private readonly float[] _predS = new float[Roll], _residS = new float[Roll];
    private readonly float[] _predT = new float[Roll], _residT = new float[Roll];
    private double _srcBitsEma = 12, _tgtBitsEma = 12;
    private double _phase;
    private DateTime _last = DateTime.Now;
    private DispatcherTimer? _timer;

    public ConvertScope()
    {
        _show = _roll;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _last = DateTime.Now;
        _timer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(33), DispatcherPriority.Render, OnTick);
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _timer?.Stop();
        _timer = null;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        double dt = Math.Min(0.05, (now - _last).TotalSeconds);
        _last = now;
        _phase += dt * (Active ? 3.0 : 1.0);
        InvalidateVisual();
    }

    private void Push(float[] win)
    {
        int m = Math.Min(win.Length, Roll);
        if (m < Roll) Array.Copy(_roll, m, _roll, 0, Roll - m);
        Array.Copy(win, Math.Max(0, win.Length - m), _roll, Roll - m, m);
    }

    public override void Render(DrawingContext dc)
    {
        double w = Bounds.Width, h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        Color teal = GetColor("Teal", Color.FromRgb(0x34, 0xCF, 0xC0));
        Color amber = GetColor("Amber", Color.FromRgb(0xE9, 0xA6, 0x3F));
        Color ink = GetColor("Ink", Color.FromRgb(0xED, 0xF1, 0xE9));
        Color mut = GetColor("Muted", Color.FromRgb(0x7D, 0x88, 0x7C));
        Color line = GetColor("Line", Color.FromRgb(0x28, 0x31, 0x2A));
        Color card = GetColor("Glass", Color.FromRgb(0x0E, 0x13, 0x11));

        var src = CodecMath.Info(SourceCodec);
        var tgt = CodecMath.Info(TargetCodec);

        // real audio when it is flowing; a gentle demo when idle
        if (CodecMath.HasSignal(_roll)) _show = _roll;
        else { CodecMath.FillDemo(_demo, _phase); _show = _demo; }

        // both formats' real compactness on the SAME reconstructed PCM
        double srcBits = Bits(src.Predictor, _predS, _residS);
        double tgtBits = Bits(tgt.Predictor, _predT, _residT);
        _srcBitsEma += (srcBits - _srcBitsEma) * 0.12;
        _tgtBitsEma += (tgtBits - _tgtBitsEma) * 0.12;

        // header: the round trip and the net size change
        Text(dc, "ROUND TRIP", 2, 0, 9, mut, bold: true);
        Text(dc, src.Name + "  ->  PCM  ->  " + tgt.Name, 2, 13, 13, ink, bold: true);
        string net = $"{_srcBitsEma:0.0}  ->  {_tgtBitsEma:0.0} bits/sample";
        var nf = MakeText(net, 12, _tgtBitsEma <= _srcBitsEma ? teal : amber);
        dc.DrawText(nf, new Point(w - nf.Width - 2, 6));
        string verdict = _tgtBitsEma < _srcBitsEma - 0.1 ? "smaller"
            : _tgtBitsEma > _srcBitsEma + 0.1 ? "larger" : "same size";
        var vf = MakeText(verdict, 10, mut);
        dc.DrawText(vf, new Point(w - vf.Width - 2, 22));

        double top = 40, bot = h - 15, bh = bot - top;
        if (bh < 24) return;
        double gap = 14;
        double cw = (w - 2 * gap) / 3;
        var rSrc = new Rect(0, top, cw, bh);
        var rPcm = new Rect(cw + gap, top, cw, bh);
        var rTgt = new Rect(2 * (cw + gap), top, cw, bh);

        DrawCard(dc, rSrc, card, line);
        DrawCard(dc, rPcm, card, line);
        DrawCard(dc, rTgt, card, line);

        // source card: decode - packed data unpacks to audio
        using (dc.PushClip(rSrc))
        {
            PackBars(dc, rSrc, _srcBitsEma / 16.0, src.Packer, teal);
        }
        Label(dc, rSrc, src.PredLabel + " -> unpack", $"{_srcBitsEma:0.0} b/s", mut);

        // PCM card: the real reconstructed audio, the shared currency
        using (dc.PushClip(rPcm))
        {
            Trace(dc, rPcm, _show, teal, 1.6);
        }
        Label(dc, rPcm, "PCM", "16.0 b/s", mut);

        // target card: encode - predict + residual, packed at its real ratio
        using (dc.PushClip(rTgt))
        {
            if (tgt.Predictor == Pred.None) StoreStage(dc, rTgt, amber, mut);
            else PackBars(dc, rTgt, _tgtBitsEma / 16.0, tgt.Packer, teal);
        }
        Label(dc, rTgt, "encode -> " + tgt.PackLabel, $"{_tgtBitsEma:0.0} b/s", mut);

        Arrow(dc, new Point(rSrc.Right + 1, top + bh / 2), new Point(rPcm.Left - 1, top + bh / 2));
        Arrow(dc, new Point(rPcm.Right + 1, top + bh / 2), new Point(rTgt.Left - 1, top + bh / 2));
    }

    private double Bits(Pred kind, float[] pred, float[] resid)
    {
        if (kind == Pred.None) return 16.0;
        CodecMath.ComputeResidual(_show, kind, pred, resid);
        return CodecMath.BitsPerSample(resid, kind);
    }

    // packed vs raw 16-bit columns; shorter packed columns = more compression
    private void PackBars(DrawingContext dc, Rect r, double ratio, CodecMath.Pack pack, Color teal)
    {
        double frac = Math.Max(0.06, Math.Min(1, ratio));
        int cols = 8; double pad = 8, cw = (r.Width - pad * 2) / (cols * 1.7);
        double baseY = r.Bottom - 8, fullH = r.Height - 18;
        var raw = new SolidColorBrush(Color.FromArgb(50, teal.R, teal.G, teal.B));
        var packed = new SolidColorBrush(pack == CodecMath.Pack.Range
            ? Color.FromRgb(0x5f, 0xE0, 0xD3)
            : teal);
        for (int i = 0; i < cols; i++)
        {
            double x = r.Left + pad + i * cw * 1.7;
            dc.DrawRectangle(raw, null, new Rect(x, baseY - fullH, cw, fullH));
            double ph = fullH * frac * (0.9 + 0.1 * Math.Sin(_phase * 3 + i));
            dc.DrawRectangle(packed, null, new Rect(x, baseY - ph, cw, ph));
        }
    }

    private void StoreStage(DrawingContext dc, Rect r, Color amber, Color mut)
    {
        Trace(dc, r, _show, amber, 1.4);
        var tick = new Pen(new SolidColorBrush(Color.FromArgb(80, mut.R, mut.G, mut.B)), 1);
        for (double bx = r.Left + 6; bx < r.Right - 4; bx += 7)
            dc.DrawLine(tick, new Point(bx, r.Bottom - 8), new Point(bx, r.Bottom - 4));
    }

    private void Label(DrawingContext dc, Rect r, string left, string right, Color mut)
    {
        Text(dc, left, r.Left + 3, r.Bottom + 1, 9, mut);
        var rf = MakeText(right, 9, mut);
        dc.DrawText(rf, new Point(r.Right - rf.Width - 3, r.Bottom + 1));
    }

    private static void Trace(DrawingContext dc, Rect r, float[] data, Color c, double thick)
    {
        double mid = r.Top + r.Height / 2, amp = r.Height * 0.42;
        double left = r.Left + 5, wide = r.Width - 10;
        var geo = new StreamGeometry();
        int n = data.Length;
        int stepPx = Math.Max(1, n / (int)Math.Max(1, wide));
        using (StreamGeometryContext ctx = geo.Open())
        {
            bool first = true;
            for (int i = 0; i < n; i += stepPx)
            {
                double v = Math.Clamp(data[i], -1.4f, 1.4f);
                var pt = new Point(left + wide * i / (n - 1.0), mid - v * amp);
                if (first) { ctx.BeginFigure(pt, false); first = false; }
                else ctx.LineTo(pt);
            }
            ctx.EndFigure(false);
        }
        var pen = new Pen(new SolidColorBrush(c), thick) { LineJoin = PenLineJoin.Round };
        dc.DrawGeometry(null, pen, geo);
    }

    private static void DrawCard(DrawingContext dc, Rect r, Color card, Color line)
    {
        dc.DrawRectangle(
            new SolidColorBrush(Color.FromArgb(190, card.R, card.G, card.B)),
            new Pen(new SolidColorBrush(line), 1),
            new RoundedRect(r, 8));
    }

    private static void Arrow(DrawingContext dc, Point a, Point b)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(150, 125, 136, 124)), 1.6)
        {
            LineCap = PenLineCap.Round,
        };
        dc.DrawLine(pen, a, b);
        dc.DrawLine(pen, b, new Point(b.X - 4, b.Y - 3));
        dc.DrawLine(pen, b, new Point(b.X - 4, b.Y + 3));
    }

    private Color GetColor(string key, Color fallback)
        => this.TryFindResource(key, ActualThemeVariant, out object? value) &&
           value is ISolidColorBrush brush
            ? brush.Color
            : fallback;

    private void Text(DrawingContext dc, string s, double x, double y, double size, Color c, bool bold = false)
        => dc.DrawText(MakeText(s, size, c, bold), new Point(x, y));

    private FormattedText MakeText(string s, double size, Color c, bool bold = false)
    {
        FontFamily family = this.TryFindResource("Mono", out object? value) && value is FontFamily mono
            ? mono
            : new FontFamily("monospace");
        return new FormattedText(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(family, FontStyle.Normal, bold ? FontWeight.Bold : FontWeight.Normal),
            size, new SolidColorBrush(c));
    }
}
