using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.VisualTree;
using CUETools.Linux.App;
using CUETools.Linux.App.Services;
using CUETools.Wpf.Services;
using Xunit;

namespace CUETools.Linux.Tests;

// SLICE-016. A bank shows every position at once, which is the whole point of
// it and also its one failure mode: it is wider than the dropdown it replaced,
// so a container that used to fit can now cut a position off.
//
// The existing scale matrix cannot see this. It asserts the PAGE does not
// overflow its viewport, and a bank inside a fixed-width column overflows the
// column silently while the page stays exactly as wide as it was. The first
// render of this slice clipped "Salvage" off the Rip page's quality bank with
// every other assertion in the suite still green.
public class SelectorLayoutTests
{
    private sealed class NullDialogs : IFileDialogService
    {
        public Task<string[]?> PickFilesAsync(string title, bool multiselect, IReadOnlyList<FilePickerGroup> groups)
            => Task.FromResult<string[]?>(null);
        public Task<string?> PickFolderAsync(string title) => Task.FromResult<string?>(null);
    }

    private sealed class DeclinePrompts : IUserPrompt
    {
        public Task<bool> ConfirmOkCancelAsync(string message, string title) => Task.FromResult(false);
    }

    // the supported widths from SLICE-013: the default, both breakpoints, and
    // the floor the layout is held at
    private static readonly (double W, double H, double Scale, string Name)[] Widths =
    {
        (1200d, 760d, 1.00, "default"),
        (1152d, 720d, 1.25, "125%"),
        (960d, 600d, 1.50, "150%"),
        (800d, 540d, 2.00, "floor"),
        (640d, 480d, 1.00, "min"),
    };

    [AvaloniaFact]
    public void NoSelectorBankEverHasAPositionCutOff()
    {
        Composition.AppGraph graph = Composition.CreateAppGraph(
            new NullDialogs(), new DeclinePrompts(), new AvaloniaUiDispatcher());
        var window = new MainWindow(new ThemeState(), graph.Verify, graph.Convert, graph);
        window.Show();

        var checkedAny = false;
        foreach (ThemeVariant variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            Avalonia.Application.Current!.RequestedThemeVariant = variant;
            foreach (var (w, h, scale, name) in Widths)
            {
                window.SetRenderScaling(scale);
                window.Width = w;
                window.Height = h;
                window.ShowRipPage();
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();

                foreach (ListBox bank in window.GetVisualDescendants()
                             .OfType<ListBox>()
                             .Where(b => b.Classes.Contains("bank")))
                {
                    if (bank.Bounds.Width <= 0)
                        continue;   // not laid out on this page

                    foreach (ListBoxItem item in bank.GetVisualDescendants().OfType<ListBoxItem>())
                    {
                        checkedAny = true;
                        Point? topLeft = item.TranslatePoint(default, bank);
                        Assert.NotNull(topLeft);
                        double right = topLeft!.Value.X + item.Bounds.Width;

                        Assert.True(right <= bank.Bounds.Width + 0.5,
                            $"{name} {variant}: bank position \"{item.Content}\" ends at {right:0.0} " +
                            $"inside a bank only {bank.Bounds.Width:0.0} wide, so it is cut off");
                        Assert.True(item.Bounds.Width > 1,
                            $"{name} {variant}: bank position \"{item.Content}\" collapsed to " +
                            $"{item.Bounds.Width:0.0} wide");
                    }
                }
            }
        }

        Assert.True(checkedAny, "no bank was laid out at all, so this test proved nothing");
        window.Close();
    }

    [AvaloniaFact]
    public void EveryBankPositionIsReachableAndSaysWhatItIs()
    {
        // a bank replaces a control that had one accessible name and a list
        // behind it with N controls that each need their own
        Composition.AppGraph graph = Composition.CreateAppGraph(
            new NullDialogs(), new DeclinePrompts(), new AvaloniaUiDispatcher());
        var window = new MainWindow(new ThemeState(), graph.Verify, graph.Convert, graph);
        window.Width = 1200;
        window.Height = 760;
        window.Show();
        window.ShowRipPage();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        var banks = window.GetVisualDescendants().OfType<ListBox>()
            .Where(b => b.Classes.Contains("bank") && b.Bounds.Width > 0).ToList();
        Assert.NotEmpty(banks);

        foreach (ListBox bank in banks)
        {
            var items = bank.GetVisualDescendants().OfType<ListBoxItem>().ToList();
            Assert.True(items.Count >= 2, "a bank with fewer than two positions is not a bank");

            // exactly one position is held down, and it is a real one
            Assert.InRange(bank.SelectedIndex, 0, items.Count - 1);

            foreach (ListBoxItem item in items)
            {
                Assert.True(item.IsVisible, $"bank position \"{item.Content}\" is not visible");
                Assert.False(string.IsNullOrWhiteSpace(item.Content?.ToString()),
                    "a bank position with no content cannot be read out or chosen");
            }
        }
        window.Close();
    }
}
