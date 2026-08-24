using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using CUETools.Linux.App;
using CUETools.Linux.App.Controls;
using CUETools.Linux.App.Services;
using CUETools.Wpf.Services;
using Xunit;

namespace CUETools.Linux.Tests;

// D-080 (4) scoped the rail's strip keys into the soft-body work from the
// start, and they were the one surface left out - Avalonia's bare Button
// selector matches by exact type and RailStripKey is a Border, so the
// most-clicked thing in the app was the only thing that stopped moving.
//
// The failure mode this guards is the quiet one from D-084 (3): the renderer
// finds its layers by NAME. Rename or restructure a layer and nothing throws,
// nothing warns, no other test fails - the keys just stop deforming, and the
// only way to notice is to press one and look.
public class RailKeyDeformTests
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

    private static MainWindow StripWindow()
    {
        Composition.AppGraph graph = Composition.CreateAppGraph(
            new NullDialogs(), new DeclinePrompts(), new AvaloniaUiDispatcher());
        var window = new MainWindow(new ThemeState(), graph.Verify, graph.Convert, graph);
        // below the 1140 breakpoint, where the rail collapses to the icon strip
        window.Width = 1000;
        window.Height = 700;
        window.Show();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        return window;
    }

    [AvaloniaFact]
    public void EveryRailKeyExposesTheLayersTheRendererLooksFor()
    {
        MainWindow window = StripWindow();
        var keys = window.GetVisualDescendants().OfType<RailStripKey>()
            .Where(k => k.Bounds.Width > 0).ToList();
        Assert.NotEmpty(keys);

        foreach (RailStripKey key in keys)
        {
            Assert.True(key.GetVisualDescendant("keyFace") is Border,
                "a rail key with no keyFace silently stops deforming");
            Assert.True(key.GetVisualDescendant("keyRecess") is Border,
                "without keyRecess a receding face reveals the page, not a housing wall");
            Assert.True(key.GetVisualDescendant("keyDip") is Border,
                "without keyDip the press has no shading, which is what carries depth here");
        }
        window.Close();
    }

    [AvaloniaFact]
    public void TheGlyphRidesInsideTheFaceSoItShearsWithIt()
    {
        // D-080 (2): the mark on a key deforms WITH the rubber. A glyph that is
        // a sibling of the face rather than a child of it would float over a
        // tilting key, which is the giveaway that broke the WPF attempt.
        MainWindow window = StripWindow();
        RailStripKey key = window.GetVisualDescendants().OfType<RailStripKey>()
            .First(k => k.Bounds.Width > 0);

        var face = (Border)key.GetVisualDescendant("keyFace")!;
        Assert.Contains(
            face.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>(),
            p => p.Data != null);
        window.Close();
    }

    [AvaloniaFact]
    public void AHeldRailKeyCarriesAProjectiveTransformNotAnAffineOne()
    {
        if (!SoftBodyKey.Enabled)
            return;   // the gate is off in CI; the parts tests above still bind

        MainWindow window = StripWindow();
        var keys = window.GetVisualDescendants().OfType<RailStripKey>()
            .Where(k => k.Bounds.Width > 0).ToList();
        Assert.NotEmpty(keys);

        RailStripKey key = keys[0];
        Point corner = key.TranslatePoint(new Point(7, 6), window) ?? default;
        window.MouseMove(corner);
        window.MouseDown(corner, MouseButton.Left);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Thread.Sleep(200);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        var face = (Border)key.GetVisualDescendant("keyFace")!;
        var matrix = Assert.IsType<MatrixTransform>(face.RenderTransform);
        // D-084 (1): a transform transition silently flattens perspective to its
        // affine part. If that ever happens here the key becomes a sliding card.
        Matrix m = matrix.Matrix;
        Assert.True(Math.Abs(m.M13) > 1e-9 || Math.Abs(m.M23) > 1e-9,
            $"the rail key's transform lost its perspective terms and is now a sliding card: " +
            $"M13={m.M13:0.000000} M23={m.M23:0.000000}");

        window.MouseUp(corner, MouseButton.Left);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        window.Close();
    }
}
