using Avalonia.Controls;
using Avalonia.Interactivity;
using CUETools.Processor;
using CUETools.Wpf.Services;
using CUETools.Wpf.ViewModels;

namespace CUETools.Linux.App.Views;

public partial class RipView : UserControl
{
    private CUEConfig? _config;
    private EncoderCatalog? _catalog;

    public RipView() => InitializeComponent();

    /// <summary>The config and catalog the codec picker works on; same
    /// pattern as ConvertView.</summary>
    public void Init(CUEConfig config, EncoderCatalog catalog)
    {
        _config = config;
        _catalog = catalog;
    }

    private async void OnCodecPickerClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not RipViewModel viewModel) return;
        if (TopLevel.GetTopLevel(this) is not Window owner) return;
        var picker = new CodecPickerWindow(
            viewModel.CodecChoices, viewModel.SelectedCodecChoice?.StableId);
        CodecChoice? chosen = await picker.ShowDialog<CodecChoice?>(owner);
        if (chosen != null) viewModel.SelectCodec(chosen);
    }
}
