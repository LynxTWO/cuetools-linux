using Avalonia.Controls;
using Avalonia.Interactivity;
using CUETools.Wpf.ViewModels;

namespace CUETools.Linux.App.Views;

public partial class NamingView : UserControl
{
    public NamingView()
    {
        InitializeComponent();
        // The tray-disc preview joins once the page is on screen; construction renders
        // the examples only (the view model documents the re-entrancy reason).
        AttachedToVisualTree += (_, _) => (DataContext as NamingViewModel)?.Refresh();
    }

    private NamingViewModel? Vm => DataContext as NamingViewModel;

    private void OnPresetChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (PresetBox.SelectedItem is string name && Vm != null)
            Vm.ApplyPreset(name);
    }

    private void OnFieldClick(object? sender, RoutedEventArgs e)
    {
        if (Vm == null || sender is not Button b || b.Content is not string field) return;
        int caret = TemplateBox.CaretIndex;
        int next = Vm.InsertField(field, caret);
        TemplateBox.CaretIndex = next;
        TemplateBox.Focus();
    }
}
