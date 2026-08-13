using Avalonia.Controls;
using Avalonia.Interactivity;
using CUETools.Processor;
using CUETools.Wpf.Services;
using CUETools.Wpf.ViewModels;

namespace CUETools.Linux.App.Views;

public partial class ConvertView : UserControl
{
    private CUEConfig? _config;
    private EncoderCatalog? _catalog;

    public ConvertView()
    {
        InitializeComponent();
    }

    /// <summary>Hand-wired composition (no DI container): the shell passes
    /// the config and catalog the picker and settings dialogs work on.</summary>
    public void Init(CUEConfig config, EncoderCatalog catalog)
    {
        _config = config;
        _catalog = catalog;
    }

    private async void OnCodecPickerClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ConvertViewModel viewModel) return;
        if (TopLevel.GetTopLevel(this) is not Window owner) return;
        var picker = new CodecPickerWindow(
            viewModel.CodecChoices, viewModel.SelectedCodecChoice?.StableId);
        CodecChoice? chosen = await picker.ShowDialog<CodecChoice?>(owner);
        if (chosen != null) viewModel.SelectCodec(chosen);
    }

    private async void OnEncoderSettingsClick(object? sender, RoutedEventArgs e)
    {
        if (_config == null || _catalog == null) return;
        if (DataContext is not ConvertViewModel viewModel) return;
        if (TopLevel.GetTopLevel(this) is not Window owner) return;
        await EncoderSettingsWindow.OpenAsync(
            owner, _config, _catalog, viewModel.SelectedFormat);
    }
}
