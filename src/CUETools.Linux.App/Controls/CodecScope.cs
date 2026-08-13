using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CUETools.Wpf.Controls;
using Pack = CUETools.Wpf.Controls.CodecMath.Pack;
using Pred = CUETools.Wpf.Controls.CodecMath.Pred;

namespace CUETools.Linux.App.Controls;

/// <summary>
/// Avalonia port of the WPF CodecScope: what the selected codec actually does
/// to audio. Each frame runs the real predictor of the codec's family (via the
/// shared CodecMath) so signal, prediction, residual, bits/sample, and ratio
/// are computed figures, not decoration. Lossy codecs get their own perceptual
/// pipeline (spectrum -> mask -> quantize -> pack) via the shared LossyMath.
/// The WPF control also consumes live rip telemetry; that feed arrives with
/// the rip slice - here the scope runs its legible demo signal, which is
/// exactly what the Convert page's idle state shows on WPF too.
/// </summary>
public sealed class CodecScope : Control
{
    public static readonly StyledProperty<string> CodecProperty =
        AvaloniaProperty.Register<CodecScope, string>(nameof(Codec), "flac");
    public static readonly StyledProperty<string> ModeProperty =
        AvaloniaProperty.Register<CodecScope, string>(nameof(Mode), "");
    public static readonly StyledProperty<bool> ActiveProperty =
        AvaloniaProperty.Register<CodecScope, bool>(nameof(Active));

    public string Codec { get => GetValue(CodecProperty); set => SetValue(CodecProperty, value); }
    public string Mode { get => GetValue(ModeProperty); set => SetValue(ModeProperty, value); }
    public bool Active { get => GetValue(ActiveProperty); set => SetValue(ActiveProperty, value); }

    private const int Roll = 640;
    private readonly float[] _demo = new float[Roll];
    private readonly float[] _pred = new float[Roll];
    private readonly float[] _resid = new float[Roll];
    private double _bitsEma = 16, _ratioEma = 1;
    private double _phase;
    private DateTime _last = DateTime.Now;
    private DispatcherTimer? _timer;

    // lossy pipeline state: a longer window for the FFT analysis + smoothed readouts
    private readonly float[] _fftDemo = new float[LossyMath.N];
    private double _kbpsEma = 192, _discEma = 50;

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
        if (!IsVisible) return;
        var now = DateTime.Now;
        double dt = Math.Min(0.05, (now - _last).TotalSeconds);
        _last = now;
        _phase += dt * (Active ? 3.0 : 1.0);
        InvalidateVisual();
    }

    public override void Render(DrawingContext dc)
    {
        double w = Bounds.Width, h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        // lossy codecs draw their own pipeline; different family, different truth
        var lossy = LossyMath.Info(Codec);
        if (lossy != null) { RenderLossy(dc, w, h, lossy); return; }

        var info = CodecMath.Info(Codec);
        Color teal = GetColor("Teal", Color.FromRgb(0x34, 0xCF, 0xC0));
        Color amber = GetColor("Amber", Color.FromRgb(0xE9, 0xA6, 0x3F));
        Color ink = GetColor("Ink", Color.FromRgb(0xED, 0xF1, 0xE9));
        Color mut = GetColor("Muted", Color.FromRgb(0x7D, 0x88, 0x7C));

        CodecMath.FillDemo(_demo, _phase);
        CodecMath.ComputeResidual(_demo, info.Predictor, _pred, _resid);
        double bits = info.Predictor == Pred.None ? 16.0 : CodecMath.BitsPerSample(_resid, info.Predictor);
        double ratio = Math.Max(0.02, Math.Min(1.0, bits / 16.0));
        _bitsEma += (bits - _bitsEma) * 0.12;
        _ratioEma += (ratio - _ratioEma) * 0.12;

        // header: name + mechanism, then the live compression readout on the right
        Text(dc, info.Name, 2, 0, 14, ink, bold: true);
        Text(dc, info.Desc, 2, 20, 10.5, mut);
        DrawCompression(dc, w, info.Packer, teal, amber, mut);

        double top = 42, bot = h - 15, bh = bot - top;
        if (bh < 24) return;

        string[] stages = info.Predictor == Pred.None
            ? new[] { "signal", info.PredLabel }
            : new[] { "signal", info.PredLabel, "residual", info.PackLabel };
        double gap = 10;
        double sw = (w - gap * (stages.Length - 1)) / stages.Length;
        for (int i = 0; i < stages.Length; i++)
        {
            double x = i * (sw + gap);
            var r = new Rect(x, top, sw, bh);
            DrawCard(dc, r);
            using (dc.PushClip(r))
            {
                DrawStage(dc, i, stages.Length, r, info, teal, amber, mut);
            }
            Text(dc, stages[i], x + 8, bot + 1, 9, mut);
            if (i < stages.Length - 1)
                Arrow(dc, new Point(x + sw + 1, top + bh / 2), new Point(x + sw + gap - 1, top + bh / 2));
        }
    }

    private void DrawStage(DrawingContext dc, int index, int count, Rect r,
        CodecMath.CodecInfo info, Color teal, Color amber, Color mut)
    {
        bool isLast = index == count - 1;
        if (index == 0) { Trace(dc, r, _demo, teal, 1.6); return; }             // signal
        if (info.Predictor == Pred.None) { StoreStage(dc, r, teal, mut); return; }
        if (isLast) { PackStage(dc, r, info.Packer, teal, mut); return; }
        if (index == 1)                                                          // predict
        {
            Trace(dc, r, _demo, Color.FromArgb(70, teal.R, teal.G, teal.B), 1.3);
            Trace(dc, r, _pred, amber, 1.6);
            return;
        }
        // residual: faint envelope of how big the signal was, then what is left
        double mid = r.Top + r.Height / 2, amp = r.Height * 0.42;
        double env = 0;
        for (int i = 0; i < Roll; i++) env = Math.Max(env, Math.Abs(_demo[i]));
        double eh = Math.Min(amp, env * amp);
        dc.DrawRectangle(
            new SolidColorBrush(Color.FromArgb(22, mut.R, mut.G, mut.B)), null,
            new Rect(r.Left + 5, mid - eh, r.Width - 10, eh * 2));
        Trace(dc, r, _resid, teal, 1.4);
    }

    private void PackStage(DrawingContext dc, Rect r, Pack pack, Color teal, Color mut)
    {
        if (pack == Pack.Range) { RangeStage(dc, r, teal, mut); return; }
        int cols = 10; double pad = 8, cw = (r.Width - pad * 2) / (cols * 1.7);
        double baseY = r.Bottom - 7, fullH = r.Height - 16;
        double frac = Math.Max(0.06, Math.Min(1, _ratioEma));
        var raw = new SolidColorBrush(Color.FromArgb(55, teal.R, teal.G, teal.B));
        var packed = new SolidColorBrush(teal);
        for (int i = 0; i < cols; i++)
        {
            double x = r.Left + pad + i * cw * 1.7;
            dc.DrawRectangle(raw, null, new Rect(x, baseY - fullH, cw, fullH));
            double ph = fullH * frac * (0.9 + 0.1 * Math.Sin(_phase * 3 + i));
            dc.DrawRectangle(packed, null, new Rect(x, baseY - ph, cw, ph));
        }
    }

    private void RangeStage(DrawingContext dc, Rect r, Color teal, Color mut)
    {
        double x0 = r.Left + 8, x1 = r.Right - 8, y = r.Top + 8, hh = (r.Height - 16) / 5;
        double lo = 0, hi = 1;
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(120, mut.R, mut.G, mut.B)), 0.8);
        for (int row = 0; row < 5; row++)
        {
            double a = x0 + (x1 - x0) * lo, b = x0 + (x1 - x0) * hi;
            var fill = new SolidColorBrush(Color.FromArgb((byte)(180 - row * 26), teal.R, teal.G, teal.B));
            dc.DrawRectangle(fill, pen, new Rect(a, y + row * hh, Math.Max(2, b - a), hh - 2));
            double span = hi - lo, pick = 0.32 + 0.30 * (0.5 + 0.5 * Math.Sin(_phase + row));
            lo += span * pick * 0.35; hi = lo + span * 0.42;
        }
    }

    private void StoreStage(DrawingContext dc, Rect r, Color teal, Color mut)
    {
        Trace(dc, r, _demo, teal, 1.6);
        var tick = new Pen(new SolidColorBrush(Color.FromArgb(80, mut.R, mut.G, mut.B)), 1);
        for (double bx = r.Left + 6; bx < r.Right - 4; bx += 7)
            dc.DrawLine(tick, new Point(bx, r.Bottom - 8), new Point(bx, r.Bottom - 4));
    }

    private void DrawCompression(DrawingContext dc, double w, Pack pack,
        Color teal, Color amber, Color mut)
    {
        string headline = pack == Pack.Store ? "16.0 bits/sample" : $"{_bitsEma:0.0} bits/sample";
        string sub = pack == Pack.Store ? "1:1 - no compression" : $"~{_ratioEma * 100:0}% of PCM";
        var ftA = MakeText(headline, 12.5, pack == Pack.Store ? amber : teal);
        var ftB = MakeText(sub, 10.5, mut);
        dc.DrawText(ftA, new Point(w - ftA.Width - 2, 1));
        dc.DrawText(ftB, new Point(w - ftB.Width - 2, 20));
        double bx = w - 150, by = 38, bw = 148;
        dc.DrawRectangle(
            new SolidColorBrush(Color.FromArgb(45, teal.R, teal.G, teal.B)), null,
            new RoundedRect(new Rect(bx, by, bw, 4), 2));
        dc.DrawRectangle(
            new SolidColorBrush(pack == Pack.Store ? amber : teal), null,
            new RoundedRect(new Rect(bx, by, bw * (pack == Pack.Store ? 1.0 : _ratioEma), 4), 2));
    }

    // ---------------- lossy pipeline (perceptual coding, a different truth) ----------------

    private void RenderLossy(DrawingContext dc, double w, double h, LossyMath.Profile prof)
    {
        Color teal = GetColor("Teal", Color.FromRgb(0x34, 0xCF, 0xC0));
        Color amber = GetColor("Amber", Color.FromRgb(0xE9, 0xA6, 0x3F));
        Color ink = GetColor("Ink", Color.FromRgb(0xED, 0xF1, 0xE9));
        Color mut = GetColor("Muted", Color.FromRgb(0x7D, 0x88, 0x7C));
        Color crit = Color.FromRgb(0xEF, 0x6D, 0x6D);

        CodecMath.FillDemo(_fftDemo, _phase);
        var a = LossyMath.Analyze(_fftDemo, prof);
        _kbpsEma += (a.EstKbps - _kbpsEma) * 0.10;
        _discEma += (a.PercentDiscarded - _discEma) * 0.10;

        string title = string.IsNullOrWhiteSpace(Mode) ? prof.Name : $"{prof.Name} - {Mode}";
        Text(dc, title, 2, 0, 14, ink, bold: true);
        string head = $"content ~{_kbpsEma:0} kbps";
        var ft = MakeText(head, 14, amber);
        dc.DrawText(ft, new Point(w - ft.Width - 2, 0));
        string sub = $"{_discEma:0}% of spectral detail inaudible - discarded";
        var ft2 = MakeText(sub, 10, mut);
        dc.DrawText(ft2, new Point(w - ft2.Width - 2, 20));

        double top = 42, bot = h - 15, bh = bot - top;
        if (bh < 24) return;

        var stages = prof.Stages;
        double gap = 10;
        double sw = (w - gap * (stages.Length - 1)) / stages.Length;
        for (int i = 0; i < stages.Length; i++)
        {
            double x = i * (sw + gap);
            var r = new Rect(x, top, sw, bh);
            DrawCard(dc, r);
            using (dc.PushClip(r))
            {
                switch (i)
                {
                    case 0: SpectrumStage(dc, r, a, withMask: false, teal, amber, crit); break;
                    case 1: SpectrumStage(dc, r, a, withMask: true, teal, amber, crit); break;
                    case 2: QuantStage(dc, r, a, teal, amber); break;
                    default: LossyPackStage(dc, r, a, prof, teal, mut); break;
                }
            }
            Text(dc, stages[i], x + 8, bot + 1, 9, mut);
            if (i < stages.Length - 1)
                Arrow(dc, new Point(x + sw + 1, top + bh / 2), new Point(x + sw + gap - 1, top + bh / 2));
        }
    }

    private static double Fx(int bin, Rect r)
    {
        double f = Math.Max(50, bin * 44100.0 / LossyMath.N);
        return r.Left + 4 + (r.Width - 8) * Math.Log(f / 50.0) / Math.Log(20000.0 / 50.0);
    }

    private static double Dy(double db, Rect r)
        => r.Top + 5 + (r.Height - 10) * Math.Min(1, Math.Max(0, -db / 96.0));

    private static void SpectrumStage(DrawingContext dc, Rect r, LossyMath.Analysis a,
        bool withMask, Color teal, Color amber, Color crit)
    {
        var keep = new Pen(new SolidColorBrush(teal), 1.2);
        var drop = new Pen(new SolidColorBrush(Color.FromArgb(120, crit.R, crit.G, crit.B)), 1.0);
        for (int k = 2; k < LossyMath.Bins; k += 2)
        {
            double x = Fx(k, r);
            double y = Dy(a.SpectrumDb[k], r);
            bool dropped = withMask && !a.Kept[k];
            dc.DrawLine(dropped ? drop : keep, new Point(x, r.Bottom - 4), new Point(x, y));
        }
        if (withMask)
        {
            var maskPen = new Pen(new SolidColorBrush(amber), 1.6);
            Point? prev = null;
            for (int k = 2; k < LossyMath.Bins; k += 4)
            {
                var p = new Point(Fx(k, r), Dy(a.MaskDb[k], r));
                if (prev != null) dc.DrawLine(maskPen, prev.Value, p);
                prev = p;
            }
        }
    }

    private static void QuantStage(DrawingContext dc, Rect r, LossyMath.Analysis a,
        Color teal, Color amber)
    {
        var q = new Pen(new SolidColorBrush(teal), 2.2);
        var tick = new SolidColorBrush(Color.FromArgb(200, amber.R, amber.G, amber.B));
        for (int k = 2; k < LossyMath.Bins; k += 4)
        {
            if (!a.Kept[k]) continue;
            double x = Fx(k, r);
            double y = Dy(a.QuantDb[k], r);
            dc.DrawLine(q, new Point(x, r.Bottom - 4), new Point(x, y));
            dc.DrawRectangle(tick, null, new Rect(x - 1.6, y - 1, 3.2, 2));
        }
    }

    private static void LossyPackStage(DrawingContext dc, Rect r, LossyMath.Analysis a,
        LossyMath.Profile prof, Color teal, Color mut)
    {
        double x = r.Left + 6, baseY = r.Bottom - 8, maxH = r.Height - 18;
        var bar = new SolidColorBrush(teal);
        var runB = new SolidColorBrush(Color.FromArgb(90, mut.R, mut.G, mut.B));
        int run = 0;
        for (int k = 2; k < LossyMath.Bins && x < r.Right - 8; k += 4)
        {
            if (!a.Kept[k]) { run++; continue; }
            if (prof.HuffmanPack)
            {
                double snr = Math.Max(0, a.SpectrumDb[k] - a.MaskDb[k]);
                double bits = Math.Max(1, Math.Log2(1 + Math.Pow(10, snr / 20.0)));
                double bw = 1.5 + bits * 0.8;
                double bhh = Math.Min(maxH, 4 + bits * (maxH / 14.0));
                dc.DrawRectangle(bar, null, new Rect(x, baseY - bhh, bw, bhh));
                x += bw + 1.5;
            }
            else
            {
                if (run > 0)
                {
                    dc.DrawRectangle(runB, null, new Rect(x, baseY - 3, Math.Min(10, 2 + run), 3));
                    x += Math.Min(10, 2 + run) + 1;
                }
                double lvl = Math.Max(0, a.QuantDb[k] + 96) / 96.0;
                double bhh = Math.Min(maxH, 4 + lvl * maxH);
                dc.DrawRectangle(bar, null, new Rect(x, baseY - bhh, 3.5, bhh));
                x += 5;
            }
            run = 0;
        }
    }

    // ---------------- shared drawing helpers ----------------

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

    private void DrawCard(DrawingContext dc, Rect r)
    {
        Color card = GetColor("Glass", Color.FromRgb(0x0E, 0x13, 0x11));
        Color line = GetColor("Line", Color.FromRgb(0x28, 0x31, 0x2A));
        dc.DrawRectangle(
            new SolidColorBrush(Color.FromArgb(190, card.R, card.G, card.B)),
            new Pen(new SolidColorBrush(line), 1),
            new RoundedRect(r, 8));
    }

    private static void Arrow(DrawingContext dc, Point a, Point b)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(140, 125, 136, 124)), 1.4)
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
