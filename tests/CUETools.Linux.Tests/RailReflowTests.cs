using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using CUETools.Linux.App.Controls;
using CUETools.Linux.App.Theme;
using Xunit;

namespace CUETools.Linux.Tests;

// SLICE-013's reflow layer: the breakpoint math, the two-column stacking
// mechanism, and the strip key's state contract. The window-level matrix
// (5 factors x 2 themes x 3 states with no-clipping assertions) is the
// harness that exercises the wiring end to end.
public class RailReflowTests
{
    [Fact]
    public void TheBreakpointsSitExactlyWhereD076PutThem()
    {
        Assert.Equal(RailLayout.Full, RailBreakpoints.For(1920));
        Assert.Equal(RailLayout.Full, RailBreakpoints.For(1140));
        Assert.Equal(RailLayout.Compact, RailBreakpoints.For(1139.9));
        Assert.Equal(RailLayout.Compact, RailBreakpoints.For(860));
        Assert.Equal(RailLayout.Floor, RailBreakpoints.For(859.9));
        Assert.Equal(RailLayout.Floor, RailBreakpoints.For(640));
    }

    [AvaloniaFact]
    public void TwoColumnReflowStacksAndRestoresLosslessly()
    {
        var grid = new Grid();
        var left = new StackPanel();
        var right = new StackPanel();
        grid.Children.Add(left);
        grid.Children.Add(right);
        var full = new Thickness(0, 58, 0, 0);
        var compact = new Thickness(0, 8, 0, 0);

        TwoColumnReflow.Apply(grid, left, right, compact: true, full, compact);
        Assert.Single(grid.ColumnDefinitions);
        Assert.Equal(2, grid.RowDefinitions.Count);
        Assert.Equal(0, Grid.GetColumn(right));
        Assert.Equal(1, Grid.GetRow(right));
        Assert.Equal(compact, right.Margin);

        TwoColumnReflow.Apply(grid, left, right, compact: false, full, compact);
        Assert.Equal(3, grid.ColumnDefinitions.Count);
        Assert.Empty(grid.RowDefinitions);
        Assert.Equal(2, Grid.GetColumn(right));
        Assert.Equal(0, Grid.GetRow(right));
        Assert.Equal(full, right.Margin);
    }

    [AvaloniaFact]
    public void AStripKeyCarriesItsNameAndLightsOnlyWhenActive()
    {
        var key = new RailStripKey(RailIcons.Rip, "Rip");
        var window = new Window { Content = key };
        window.Show();

        Assert.Equal("Rip", ToolTip.GetTip(key));
        key.SetActive(false);
        Assert.False(key.IsActiveKey);

        key.SetActive(true);
        Assert.True(key.IsActiveKey);
        window.Close();
    }
}
