using Avalonia.Controls;
using Avalonia.Interactivity;
using CUETools.Wpf.Services;
using CUETools.Wpf.ViewModels;

namespace CUETools.Linux.App.Views;

public partial class QueueView : UserControl
{
    public QueueView()
    {
        InitializeComponent();
    }

    private async void OnCodecPickerClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not QueueViewModel viewModel) return;
        if (TopLevel.GetTopLevel(this) is not Window owner) return;
        var picker = new CodecPickerWindow(
            viewModel.CodecChoices, viewModel.SelectedCodecChoice?.StableId);
        CodecChoice? chosen = await picker.ShowDialog<CodecChoice?>(owner);
        if (chosen != null) viewModel.SelectCodec(chosen);
    }
}
