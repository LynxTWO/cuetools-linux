using Avalonia.Threading;

namespace CUETools.Linux.App.Controls;

/// <summary>
/// A short value ramp on the UI thread, used for the soft-body key's collapse
/// and rebound (SLICE-015). Deliberately not an Avalonia Animation: the value
/// drives a matrix that is recomputed per frame from the deformation model, and
/// a finished Animation reverts its property to the base value - the exact trap
/// that flashed the old theme at the end of every crossfade (see ThemeCrossfade).
/// </summary>
public sealed class DispatcherTimerLoop
{
    private readonly DispatcherTimer _timer;

    private DispatcherTimerLoop(DispatcherTimer timer) => _timer = timer;

    public void Stop() => _timer.Stop();

    /// <summary>Ramp from <paramref name="from"/> to <paramref name="to"/> over
    /// <paramref name="duration"/>, reporting every frame. With
    /// <paramref name="overshoot"/> the tail springs past the target and settles
    /// back, which is what rubber does when a real press is released.</summary>
    public static DispatcherTimerLoop Ramp(
        double from, double to, TimeSpan duration, Action<double> report, bool overshoot = false)
    {
        var started = DateTime.UtcNow;
        DispatcherTimer? timer = null;
        timer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render, (_, _) =>
        {
            double t = duration.TotalMilliseconds <= 0
                ? 1
                : Math.Clamp((DateTime.UtcNow - started).TotalMilliseconds / duration.TotalMilliseconds, 0, 1);
            double eased = overshoot ? Overshoot(t) : 1 - Math.Pow(1 - t, 3);
            report(from + (to - from) * eased);
            if (t >= 1)
            {
                timer!.Stop();
                report(to);
            }
        });
        timer.Start();
        return new DispatcherTimerLoop(timer);
    }

    /// <summary>A damped spring that passes its target and comes back, the way a
    /// rubber cap rebounds past flat before settling.</summary>
    private static double Overshoot(double t)
    {
        if (t >= 1) return 1;
        const double zeta = 0.55, omega = 9.0;
        double envelope = Math.Exp(-zeta * omega * t);
        double damped = omega * Math.Sqrt(1 - zeta * zeta);
        return 1 - envelope * (Math.Cos(damped * t) + (zeta * omega / damped) * Math.Sin(damped * t));
    }
}
