using Avalonia.Controls;
using Avalonia.Interactivity;
using CUETools.Linux.App.Services;
using CUETools.Processor;
using CUETools.Wpf.Services;
using CUETools.Wpf.ViewModels;

namespace CUETools.Linux.App.Views;

public partial class RipView : UserControl
{
    private CUEConfig? _config;
    private EncoderCatalog? _catalog;
    private IAlbumArtService? _art;

    public RipView() => InitializeComponent();

    /// <summary>The config, catalog, and art service the pickers work on;
    /// same pattern as ConvertView.</summary>
    public void Init(CUEConfig config, EncoderCatalog catalog, IAlbumArtService art)
    {
        _config = config;
        _catalog = catalog;
        _art = art;
    }

    private async void OnArtworkClick(object? sender, RoutedEventArgs e)
    {
        if (_art == null) return;
        if (DataContext is not RipViewModel viewModel) return;
        if (TopLevel.GetTopLevel(this) is not Window owner) return;
        var browser = new ArtworkBrowserWindow(
            viewModel.ArtworkCandidates,
            viewModel.SelectedArtwork?.CandidateId,
            _art,
            new AvaloniaArtworkPreviewFactory());
        var chosen = await browser.ShowDialog<CUETools.Wpf.Services.Artwork.ArtworkCandidate?>(owner);
        if (chosen != null) await viewModel.SelectArtworkAsync(chosen);
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
