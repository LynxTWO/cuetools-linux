using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CUETools.Processor;
using CUETools.Wpf.Services;
using CUETools.Wpf.ViewModels;

namespace CUETools.Linux.App.Views;

/// <summary>
/// Per-encoder settings dialog (port of the WPF EncoderSettingsWindow):
/// TYPE for two-faced formats, ENCODER when several implementations can run,
/// the compression/quality mode, and every advanced option the codec exposes.
/// </summary>
public partial class EncoderSettingsWindow : Window
{
    public EncoderSettingsWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    /// <summary>Open for a format, resolving lossy-ness the same way the
    /// format lists do. A two-faced format carries the TYPE picker; choosing
    /// the other type persists the choice and rebuilds this dialog around the
    /// other encoder. A user-invoked dialog must not fail silently, so a
    /// failure surfaces as a plain message dialog.</summary>
    public static async Task OpenAsync(
        Window owner, CUEConfig config, EncoderCatalog catalog, string format)
    {
        if (string.IsNullOrWhiteSpace(format) || !config.formats.ContainsKey(format)) return;
        bool lossy = catalog.IsLossyFormat(config.formats[format]);
        try
        {
            var window = new EncoderSettingsWindow();
            Wire(window, config, catalog, format, lossy);
            await window.ShowDialog(owner);
        }
        catch (Exception ex)
        {
            var message = new Window
            {
                Title = "Encoder settings",
                Width = 420,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new TextBlock
                {
                    Text = "Could not open encoder settings: " + ex.Message,
                    Margin = new Avalonia.Thickness(18),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
            };
            await message.ShowDialog(owner);
        }
    }

    private static void Wire(EncoderSettingsWindow window, CUEConfig config,
        EncoderCatalog catalog, string format, bool lossy)
    {
        var viewModel = new EncoderSettingsViewModel(config, catalog, format, lossy);
        window.DataContext = viewModel;
        viewModel.HasTypeChoice = catalog.HasBothTypes(config.formats[format]);
        viewModel.IsLossyType = lossy;
        viewModel.TypeChanged += chooseLossy =>
        {
            catalog.SetFormatType(format, chooseLossy);   // persists + rebuilds the format lists
            Wire(window, config, catalog, format, chooseLossy);
        };
        viewModel.EncoderChanged += encoder =>
        {
            catalog.SetFormatEncoder(config, format, !lossy, encoder);
            Wire(window, config, catalog, format, lossy);
        };
    }
}
