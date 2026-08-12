using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace CUETools.Linux.App.Controls;

/// <summary>
/// Avalonia port of the WPF RepairScope (fork Controls/RepairScope.cs, per
/// the disc-read-visualization skill): CTDB's Reed-Solomon parity repair,
/// drawn from the real CDRepairFix numbers. The map is a downsample of the
/// true AffectedSectorArray (damage shows exactly where the disc was
/// damaged); Samples/Sectors/Npar are the literal corrected-sample,
/// damaged-sector, and parity-depth values; the pipeline chips reflect the
/// real RS decode (syndrome/locate/Chien/Forney light at verify, apply
/// lights only on repair). States: recoverable (amber) -> repairing (green
/// sweep tied to the real Progress) -> repaired (green). Honesty rule: the
/// numbers are literal; only the animation is smoothed.
/// </summary>
public sealed class RepairScope : Control
{
    public static readonly StyledProperty<bool> ActiveProperty =
        AvaloniaProperty.Register<RepairScope, bool>(nameof(Active));
    public static readonly StyledProperty<bool> AppliedProperty =
        AvaloniaProperty.Register<RepairScope, bool>(nameof(Applied));
    public static readonly StyledProperty<bool> RecoverableProperty =
        AvaloniaProperty.Register<RepairScope, bool>(nameof(Recoverable));
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<RepairScope, double>(nameof(Progress));
    public static readonly StyledProperty<int> SamplesProperty =
        AvaloniaProperty.Register<RepairScope, int>(nameof(Samples));
    public static readonly StyledProperty<int> SectorsProperty =
        AvaloniaProperty.Register<RepairScope, int>(nameof(Sectors));
    public static readonly StyledProperty<int> NparProperty =
        AvaloniaProperty.Register<RepairScope, int>(nameof(Npar));
    public static readonly StyledProperty<double[]?> MapProperty =
        AvaloniaProperty.Register<RepairScope, double[]?>(nameof(Map));
    public static readonly StyledProperty<object?> MapSourceProperty =
        AvaloniaProperty.Register<RepairScope, object?>(nameof(MapSource));

    public bool Active { get => GetValue(ActiveProperty); set => SetValue(ActiveProperty, value); }
    public bool Applied { get => GetValue(AppliedProperty); set => SetValue(AppliedProperty, value); }
    public bool Recoverable { get => GetValue(RecoverableProperty); set => SetValue(RecoverableProperty, value); }
    public double Progress { get => GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }
    public int Samples { get => GetValue(SamplesProperty); set => SetValue(SamplesProperty, value); }
    public int Sectors { get => GetValue(SectorsProperty); set => SetValue(SectorsProperty, value); }
    public int Npar { get => GetValue(NparProperty); set => SetValue(NparProperty, value); }
    public double[]? Map { get => GetValue(MapProperty); set => SetValue(MapProperty, value); }
    public object? MapSource { get => GetValue(MapSourceProperty); set => SetValue(MapSourceProperty, value); }

    private static readonly string[] Stages = { "syndrome", "locate", "Chien", "Forney", "apply" };

    private double _sweep;
    private double _phase;
    private DateTime _last = DateTime.Now;
    private DispatcherTimer? _timer;

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
        double target = Applied ? 1.0 : Active ? Math.Clamp(Progress, 0, 1) : 0.0;
        _sweep += (target - _sweep) * 0.15;
        _phase += dt * 2.2;
        InvalidateVisual();
    }

    public override void Render(DrawingContext dc)
    {
        double w = Bounds.Width, h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        Color ink = GetColor("InkDim", Color.FromRgb(0xD4, 0xDC, 0xD2));
        Color mut = GetColor("Muted", Color.FromRgb(0x7C, 0x8A, 0x84));
        Color trackColor = GetColor("Glass", Color.FromRgb(0x10, 0x16, 0x14));
        Color teal = GetColor("StatusAccent", Color.FromRgb(0x34, 0xCF, 0xC0));
        Color amber = GetColor("StatusWarning", Color.FromRgb(0xE9, 0xA6, 0x3F));
        Color critical = GetColor("StatusDanger", Color.FromRgb(0xEF, 0x6D, 0x6D));
        Color good = GetColor("StatusGood", Color.FromRgb(0x5C, 0xCB, 0x8B));

        // ---- headline ----
        string headline = Applied
            ? $"Recovered {Fmt(Samples)} samples across {Sectors} sector" + (Sectors == 1 ? "" : "s")
            : Active
                ? "Reconstructing damaged sectors from parity..."
                : $"{Fmt(Samples)} samples across {Sectors} sector" + (Sectors == 1 ? "" : "s") + " - recoverable from parity";
        Color hColor = Applied ? good : amber;
        Text(dc, headline, 1, 0, 14, hColor, bold: true);

        // state pill (right)
        string pill = Applied ? "REPAIRED" : Active ? "REPAIRING" : "REPAIRABLE";
        Color pc = Applied ? good : amber;
        var pillFt = MakeText(pill, 9.5, pc, bold: true);
        double pillW = pillFt.Width + 16, pillH = 17, pillX = w - pillW - 1;
        dc.DrawRectangle(
            new SolidColorBrush(Color.FromArgb(28, pc.R, pc.G, pc.B)),
            new Pen(new SolidColorBrush(Color.FromArgb(150, pc.R, pc.G, pc.B)), 1),
            new RoundedRect(new Rect(pillX, 1, pillW, pillH), 8));
        dc.DrawText(pillFt, new Point(pillX + 8, 3));

        // parity structure sub-line (real numbers)
        string sub = Npar > 0
            ? $"Reed-Solomon  .  npar={Npar} parity symbols / 10-sector stride  .  GF(2^16)"
            : "Reed-Solomon parity";
        Text(dc, sub, 1, 20, 10.5, mut);

        // ---- damage / repair sector strip (the real AffectedSectorArray) ----
        double bandY = 46, bandH = 22, bandL = 1, bandR = w - 1, bandW = bandR - bandL;
        var band = new Rect(bandL, bandY, bandW, bandH);
        dc.DrawRectangle(new SolidColorBrush(trackColor), null, new RoundedRect(band, 4));

        var map = MapSource as double[] ?? Map;
        using (dc.PushClip(new RoundedRect(band, 4)))
        {
            if (map != null && map.Length > 0)
            {
                int bins = map.Length;
                double bw = bandW / bins;
                for (int b = 0; b < bins; b++)
                {
                    double d = map[b];
                    if (d <= 0) continue;
                    double cx = (b + 0.5) / bins;
                    bool recovered = Applied || (Active && cx <= _sweep);
                    Color c = recovered ? good : Lerp(amber, critical, d);
                    byte al = (byte)(90 + 150 * Math.Min(1, 0.4 + 0.6 * d));
                    double x = bandL + b * bw;
                    dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(al, c.R, c.G, c.B)), null,
                        new Rect(x, bandY + 2, Math.Max(1.0, bw - 0.5), bandH - 4));
                }
                if (Active && _sweep > 0.001 && _sweep < 0.999)
                {
                    double hx = bandL + _sweep * bandW;
                    dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(60, good.R, good.G, good.B)), null,
                        new Rect(hx - 5, bandY, 10, bandH));
                    dc.DrawLine(new Pen(new SolidColorBrush(good), 1.8),
                        new Point(hx, bandY - 1), new Point(hx, bandY + bandH + 1));
                }
            }
        }
        dc.DrawRectangle(null,
            new Pen(new SolidColorBrush(Color.FromArgb(60, ink.R, ink.G, ink.B)), 1),
            new RoundedRect(band, 4));
        Text(dc, "disc  (inside", 1, bandY + bandH + 2, 8.5, mut);
        var outFt = MakeText("outside)", 8.5, mut);
        dc.DrawText(outFt, new Point(bandR - outFt.Width, bandY + bandH + 2));

        // ---- RS pipeline: first four stages computed at verify, "apply" is the repair write ----
        double py = h - 24, gap = 8;
        double chipW = (bandW - gap * (Stages.Length - 1)) / Stages.Length;
        for (int i = 0; i < Stages.Length; i++)
        {
            double x = bandL + i * (chipW + gap);
            bool isApply = i == Stages.Length - 1;
            bool done = isApply ? Applied : (Recoverable || Active || Applied);
            bool running = isApply && Active;
            Color c = done ? (isApply ? good : teal) : mut;
            double pulse = running ? 0.5 + 0.5 * Math.Sin(_phase * 2) : 1.0;
            byte fill = (byte)((done ? 34 : 16) * pulse + 6);
            dc.DrawRectangle(
                new SolidColorBrush(Color.FromArgb(fill, c.R, c.G, c.B)),
                new Pen(new SolidColorBrush(Color.FromArgb((byte)(done ? 150 : 60), c.R, c.G, c.B)), 1),
                new RoundedRect(new Rect(x, py, chipW, 18), 5));
            var ft = MakeText(Stages[i], 9.5, done ? c : mut);
            dc.DrawText(ft, new Point(x + (chipW - ft.Width) / 2, py + 3));
            if (i < Stages.Length - 1)
            {
                double ax = x + chipW + gap / 2;
                dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(120, mut.R, mut.G, mut.B)), 1),
                    new Point(ax - 2.5, py + 9), new Point(ax + 2.5, py + 9));
            }
        }
    }

    private Color GetColor(string key, Color fallback)
        => this.TryFindResource(key, ActualThemeVariant, out object? value) &&
           value is ISolidColorBrush brush
            ? brush.Color
            : fallback;

    private static string Fmt(int n) => n.ToString("#,0", CultureInfo.InvariantCulture);

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

    private static Color Lerp(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return Color.FromRgb(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t));
    }
}
