using Avalonia.Controls;
using Avalonia.Interactivity;
using CUETools.Linux.App.Services;

namespace CUETools.Linux.App.Views;

/// <summary>The preview-diff gate (D-048): every change the database release
/// would make, one approve per album, nothing written on cancel.</summary>
public partial class EnrichmentDialog : Window
{
    public EnrichmentDialog()
    {
        InitializeComponent();
    }

    public EnrichmentDialog(EnrichmentProposal proposal)
        : this()
    {
        ChangeList.ItemsSource = proposal.Changes;
        ProviderLine.Text = string.IsNullOrWhiteSpace(proposal.InfoUrl)
            ? $"Source: {proposal.Provider}"
            : $"Source: {proposal.Provider} ({proposal.InfoUrl})";
    }

    private void OnApproveClick(object? sender, RoutedEventArgs e) => Close(true);

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);
}
