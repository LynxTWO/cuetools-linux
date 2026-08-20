using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using CUETools.Linux.App.Views;
using CUETools.Wpf.ViewModels;
using Xunit;

namespace CUETools.Linux.Tests;

// The Explore page ("How a CD Works"). The stage is a 2D pan-and-zoom disc on
// this head (HardwareAccelerated3D is false here); the page must not promise
// the WPF head's 3D orbit.
public class ExplorePageTests
{
    [Fact]
    public void TheSubtitleDescribesTheStageTheHeadActuallyHas()
    {
        var flat = new ExploreViewModel(orbital3D: false);
        Assert.Contains("drag to move", flat.Subtitle);
        Assert.DoesNotContain("orbit", flat.Subtitle);

        var orbital = new ExploreViewModel(orbital3D: true);
        Assert.Contains("orbit", orbital.Subtitle);
    }

    [AvaloniaFact]
    public void ThePageMaterializesWithItsStageAndLesson()
    {
        var window = new Window
        {
            Content = new ExploreView { DataContext = new ExploreViewModel(orbital3D: false) }
        };
        window.Show();
        Assert.NotNull(window.Content);
        window.Close();
    }
}
