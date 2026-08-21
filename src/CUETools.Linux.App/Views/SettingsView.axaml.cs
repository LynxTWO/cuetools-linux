using Avalonia.Controls;

namespace CUETools.Linux.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    /// <summary>SLICE-013: below the breakpoint the right column stacks under the
    /// left. Driven by the main window's layout state, not the view's own width,
    /// so the whole shell changes shape on one threshold.</summary>
    public void SetCompact(bool compact)
        => CUETools.Linux.App.Controls.TwoColumnReflow.Apply(
            ColumnsGrid, LeftColumn, RightColumn, compact,
            new Avalonia.Thickness(0, 58, 0, 0), new Avalonia.Thickness(0, 8, 0, 0));
}
