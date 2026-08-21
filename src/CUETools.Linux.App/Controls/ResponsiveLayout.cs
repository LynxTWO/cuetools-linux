using Avalonia;
using Avalonia.Controls;

namespace CUETools.Linux.App.Controls;

/// <summary>
/// The two-column page's compact mechanism (SLICE-013): below the breakpoint
/// the right column moves under the left one, and back again. One helper so
/// every two-column page stacks the same way (D-076's "same mechanism for
/// any future two-column page").
/// </summary>
public static class TwoColumnReflow
{
    public static void Apply(
        Grid grid, Control left, Control right, bool compact,
        Thickness rightFullMargin, Thickness rightCompactMargin)
    {
        grid.ColumnDefinitions.Clear();
        grid.RowDefinitions.Clear();
        if (compact)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            Grid.SetColumn(left, 0);
            Grid.SetRow(left, 0);
            Grid.SetColumn(right, 0);
            Grid.SetRow(right, 1);
            right.Margin = rightCompactMargin;
        }
        else
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(24)));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            Grid.SetColumn(left, 0);
            Grid.SetRow(left, 0);
            Grid.SetColumn(right, 2);
            Grid.SetRow(right, 0);
            right.Margin = rightFullMargin;
        }
    }
}
