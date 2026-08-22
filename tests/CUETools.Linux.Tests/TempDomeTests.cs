using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using CUETools.Linux.App.Controls;
using Xunit;

namespace CUETools.Linux.Tests;

public class TempDomeTests
{
    private static void Deform(Button b, Point press, double amount)
    {
        foreach (Visual v in b.GetVisualDescendants())
        {
            if (v is Border r && r.Name == "keyRecess") r.Opacity = amount > 0 ? 1 : 0;
            if (v is Border f && f.Name == "keyFace")
            {
                f.Transitions = null;
                f.RenderTransform = amount <= 0 ? null : new MatrixTransform(
                    SoftBodyKey.Homography(press, new Size(b.Bounds.Width, b.Bounds.Height), amount));
            }
            if (v is Border d && d.Name == "keyDip")
            {
                if (amount <= 0) { d.Opacity = 0; continue; }
                double w = b.Bounds.Width, h = b.Bounds.Height;
                bool dark = b.ActualThemeVariant != ThemeVariant.Light;
                var br = new RadialGradientBrush
                {
                    GradientOrigin = new RelativePoint(press.X / w, press.Y / h, RelativeUnit.Relative),
                    Center = new RelativePoint(press.X / w, press.Y / h, RelativeUnit.Relative),
                    RadiusX = new RelativeScalar(SoftBodyModel.DimpleSigma * 5.5 / w, RelativeUnit.Relative),
                    RadiusY = new RelativeScalar(SoftBodyModel.DimpleSigma * 5.5 / h, RelativeUnit.Relative),
                };
                foreach (var st in dark
                    ? new[]
                    {
                        new GradientStop(Color.FromArgb(0x86, 0, 0, 0), 0),
                        new GradientStop(Color.FromArgb(0x60, 0, 0, 0), 0.42),
                        new GradientStop(Color.FromArgb(0x24, 0, 0, 0), 0.74),
                        new GradientStop(Color.FromArgb(0x00, 0, 0, 0), 1),
                    }
                    : new[]
                    {
                        new GradientStop(Color.FromArgb(0x4E, 0, 0, 0), 0),
                        new GradientStop(Color.FromArgb(0x38, 0, 0, 0), 0.38),
                        new GradientStop(Color.FromArgb(0x18, 0, 0, 0), 0.70),
                        new GradientStop(Color.FromArgb(0x00, 0, 0, 0), 1),
                    }) br.GradientStops.Add(st);
                d.Background = br;
                d.Opacity = amount;
            }
        }
    }

    [AvaloniaFact]
    public void CaptureDome()
    {
        foreach (var (variant, tname) in new[] { (ThemeVariant.Dark, "dark"), (ThemeVariant.Light, "light") })
        {
            Avalonia.Application.Current!.RequestedThemeVariant = variant;
            var strip = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Margin = new Thickness(14) };
            var made = new List<(Button, Point?)>();
            foreach (var (cap, press, primary) in new (string, Point?, bool)[]
            {
                ("at rest", null, true),
                ("top-left corner", new Point(8, 6), true),
                ("dead centre", new Point(60, 16), false),
                ("bottom-right", new Point(112, 26), false),
            })
            {
                var col = new StackPanel { Spacing = 5, Margin = new Thickness(8, 0) };
                var b = new Button { Content = "Test & Copy", Width = 120, Height = 32, Classes = { "transport" } };
                if (primary) b.Classes.Add("primary");
                col.Children.Add(b);
                col.Children.Add(new TextBlock
                {
                    Text = cap, FontSize = 7,
                    Foreground = new SolidColorBrush(variant == ThemeVariant.Dark ? Color.Parse("#B1BCAE") : Color.Parse("#414A40")),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                });
                strip.Children.Add(col);
                made.Add((b, press));
            }
            var win = new Window
            {
                SizeToContent = SizeToContent.WidthAndHeight,
                Background = new SolidColorBrush(variant == ThemeVariant.Dark ? Color.Parse("#0C0F0D") : Color.Parse("#E7ECE2")),
                Content = new LayoutTransformControl { LayoutTransform = new ScaleTransform(3, 3), Child = strip },
            };
            win.Show();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            foreach (var (b, p) in made) Deform(b, p ?? default, p is null ? 0 : 1);
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            System.Threading.Thread.Sleep(150);
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            win.CaptureRenderedFrame()!.Save(
                $"/tmp/claude-1000/-home-daniel-boyd-DEV-apps-cuetools-2026/a4c12d28-e1ba-49c5-a3b2-cd67a1f5792c/scratchpad/dome-{tname}.png",
                new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
            win.Close();
        }
    }
}
