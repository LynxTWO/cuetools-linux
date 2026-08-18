using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CUETools.Wpf.Models;

namespace CUETools.Linux.App.Controls;

/// <summary>
/// SLICE-010 track strip: one segment per track, width proportional to track duration,
/// drawn from TrackItem state only. Test fill renders hollow; Copy fill renders solid
/// inside the retained hollow Test outline (D-057). Amber top ticks mark tracks with
/// re-read windows, red edges mark unrecoverable sectors (D-058: routine corrections do
/// not mark). Every count shown in the tooltip is the literal engine value.
/// </summary>
public sealed class TrackStrip : Control
{
    public static readonly StyledProperty<System.Collections.IEnumerable?> TracksProperty =
        AvaloniaProperty.Register<TrackStrip, System.Collections.IEnumerable?>(nameof(Tracks));
    public static readonly StyledProperty<bool> CurrentHollowProperty =
        AvaloniaProperty.Register<TrackStrip, bool>(nameof(CurrentHollow));
    public static readonly StyledProperty<bool> ShowRetainedProperty =
        AvaloniaProperty.Register<TrackStrip, bool>(nameof(ShowRetained));

    public System.Collections.IEnumerable? Tracks { get => GetValue(TracksProperty); set => SetValue(TracksProperty, value); }

    /// <summary>Test phase: the growing fill is an outline, not a solid.</summary>
    public bool CurrentHollow { get => GetValue(CurrentHollowProperty); set => SetValue(CurrentHollowProperty, value); }

    /// <summary>Copy or third-read phase: draw the retained Test outline under the solid fill.</summary>
    public bool ShowRetained { get => GetValue(ShowRetainedProperty); set => SetValue(ShowRetainedProperty, value); }

    private readonly List<TrackItem> _items = new();

    static TrackStrip()
    {
        AffectsRender<TrackStrip>(TracksProperty, CurrentHollowProperty, ShowRetainedProperty);
        TracksProperty.Changed.AddClassHandler<TrackStrip>((c, _) => c.Rebind());
    }

    private void Rebind()
    {
        foreach (TrackItem old in _items) old.PropertyChanged -= OnItemChanged;
        _items.Clear();
        if (Tracks is INotifyCollectionChanged watch)
        {
            watch.CollectionChanged -= OnCollectionChanged;
            watch.CollectionChanged += OnCollectionChanged;
        }
        if (Tracks != null)
            foreach (object o in Tracks)
                if (o is TrackItem t) { _items.Add(t); t.PropertyChanged += OnItemChanged; }
        InvalidateVisual();
    }

    private void OnCollectionChanged(object? s, NotifyCollectionChangedEventArgs e) => Rebind();

    private void OnItemChanged(object? s, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TrackItem.Progress) or nameof(TrackItem.TestProgress)
            or nameof(TrackItem.Active) or nameof(TrackItem.RereadWindows)
            or nameof(TrackItem.UnrecoverableSectors))
            InvalidateVisual();
    }

    private (double start, double width)[] Layout(double totalWidth)
    {
        var spans = new (double, double)[_items.Count];
        double totalSec = 0;
        foreach (TrackItem t in _items) totalSec += t.Length.TotalSeconds;
        if (totalSec <= 0) return spans;
        const double gap = 1.0;
        double x = 0;
        for (int i = 0; i < _items.Count; i++)
        {
            double w = totalWidth * (_items[i].Length.TotalSeconds / totalSec);
            spans[i] = (x, Math.Max(0, w - gap));
            x += w;
        }
        return spans;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        double w = Bounds.Width, h = Bounds.Height;
        if (w <= 0 || h <= 0 || _items.Count == 0) return;

        IBrush face = ResolveBrush("Glass", new SolidColorBrush(Color.FromArgb(40, 128, 128, 128)));
        IBrush fill = ResolveBrush("StatusAccent", Brushes.Teal);
        IBrush amber = ResolveBrush("StatusWarning", Brushes.Orange);
        IBrush danger = ResolveBrush("StatusDanger", Brushes.Red);
        IBrush ink = ResolveBrush("Ink", Brushes.White);
        var outline = new Pen(fill, 1.2);
        var activePen = new Pen(ink, 1.4);

        (double start, double width)[] spans = Layout(w);
        for (int i = 0; i < _items.Count; i++)
        {
            TrackItem t = _items[i];
            (double x, double sw) = spans[i];
            if (sw <= 0) continue;
            var seg = new Rect(x, 2, sw, h - 4);
            context.FillRectangle(face, seg);

            // Retained hollow Test outline under the Copy fill (D-057).
            if (ShowRetained && t.TestProgress > 0)
                context.DrawRectangle(null, outline,
                    new Rect(seg.X, seg.Y, seg.Width * Math.Clamp(t.TestProgress, 0, 1), seg.Height));

            if (t.Progress > 0)
            {
                var fillRect = new Rect(seg.X, seg.Y, seg.Width * Math.Clamp(t.Progress, 0, 1), seg.Height);
                if (CurrentHollow)
                    context.DrawRectangle(null, outline, fillRect);
                else
                    context.FillRectangle(fill, fillRect.Deflate(ShowRetained ? 1.6 : 0));
            }

            // Amber tick: this track needed re-read windows (literal count in the tooltip).
            if (t.RereadWindows > 0)
                context.FillRectangle(amber, new Rect(seg.X, 0, Math.Min(seg.Width, 10), 2));

            // Red edge: unrecoverable sectors, the terminal state Salvage leaves behind.
            if (t.UnrecoverableSectors > 0)
                context.DrawRectangle(null, new Pen(danger, 1.4), seg);

            if (t.Active)
                context.DrawRectangle(null, activePen,
                    new Rect(seg.X, h - 2.4, seg.Width, 1.6));
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        double px = e.GetPosition(this).X;
        (double start, double width)[] spans = Layout(Bounds.Width);
        for (int i = 0; i < spans.Length; i++)
        {
            if (px >= spans[i].start && px <= spans[i].start + spans[i].width)
            {
                ToolTip.SetTip(this, _items[i].StripTip);
                return;
            }
        }
        ToolTip.SetTip(this, null);
    }

    private IBrush ResolveBrush(string key, IBrush fallback)
        => this.TryFindResource(key, out object? v) && v is IBrush b ? b : fallback;
}

/// <summary>
/// SLICE-010 pass lane: literal pass ticks for the window being re-read right now,
/// window-scoped by design (D-058). Renders nothing at zero; the host hides the whole
/// lane for Burst, because absence is the honest display of one-pass mode.
/// </summary>
public sealed class PassLane : Control
{
    public static readonly StyledProperty<int> TicksProperty =
        AvaloniaProperty.Register<PassLane, int>(nameof(Ticks));
    public static readonly StyledProperty<int> MaxProperty =
        AvaloniaProperty.Register<PassLane, int>(nameof(Max), 16);

    public int Ticks { get => GetValue(TicksProperty); set => SetValue(TicksProperty, value); }
    public int Max { get => GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    static PassLane() => AffectsRender<PassLane>(TicksProperty, MaxProperty);

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        double w = Bounds.Width, h = Bounds.Height;
        int max = Math.Max(1, Max);
        if (w <= 0 || h <= 0) return;

        IBrush dim = this.TryFindResource("Glass", out object? g) && g is IBrush gb
            ? gb : new SolidColorBrush(Color.FromArgb(40, 128, 128, 128));
        IBrush lit = this.TryFindResource("StatusWarning", out object? a) && a is IBrush ab
            ? ab : Brushes.Orange;

        double step = w / max;
        double tick = Math.Max(1.5, step - 2);
        for (int i = 0; i < max; i++)
        {
            var r = new Rect(i * step, 1, tick, h - 2);
            context.FillRectangle(i < Ticks ? lit : dim, r);
        }
    }
}
