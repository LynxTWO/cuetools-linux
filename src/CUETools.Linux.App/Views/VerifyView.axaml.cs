using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CUETools.Wpf.ViewModels;

namespace CUETools.Linux.App.Views;

public partial class VerifyView : UserControl
{
    private CUETools.Linux.App.Services.IEnrichmentService? _enrichment;

    public VerifyView()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    /// <summary>Hand-wired composition: the shell passes the enrichment
    /// service the disc cards' Enrich action uses.</summary>
    public void InitEnrichment(CUETools.Linux.App.Services.IEnrichmentService enrichment)
        => _enrichment = enrichment;

    private async void OnEnrichClick(object? sender, RoutedEventArgs e)
    {
        if (_enrichment == null) return;
        if (sender is not Button { Tag: CUETools.Wpf.ViewModels.VerifyDiscViewModel disc }) return;
        if (TopLevel.GetTopLevel(this) is not Window owner) return;

        CUETools.Linux.App.Services.EnrichmentProposal? proposal;
        try
        {
            string path = disc.Source.Path;
            proposal = await Task.Run(() => _enrichment.Propose(path));
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(owner, "Enrich metadata",
                "The lookup failed: " + ex.Message);
            return;
        }

        if (proposal == null)
        {
            await ShowMessageAsync(owner, "Enrich metadata",
                "No database release was found for this album.");
            return;
        }
        if (!proposal.HasChanges)
        {
            await ShowMessageAsync(owner, "Enrich metadata",
                "This album already matches the database release - nothing to change.");
            return;
        }

        var dialog = new EnrichmentDialog(proposal);
        bool approved = await dialog.ShowDialog<bool>(owner);
        if (!approved) return;
        try
        {
            int files = await Task.Run(() => _enrichment.Apply(proposal));
            await ShowMessageAsync(owner, "Enrich metadata",
                $"Applied {proposal.Changes.Count} change(s) across {files} file(s).");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(owner, "Enrich metadata",
                "Applying failed: " + ex.Message);
        }
    }

    private static async Task ShowMessageAsync(Window owner, string title, string message)
    {
        var window = new Window
        {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new TextBlock
            {
                Text = message,
                Margin = new Avalonia.Thickness(18),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            },
        };
        await window.ShowDialog(owner);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not VerifyViewModel viewModel) return;
        var paths = e.DataTransfer.TryGetFiles()
            ?.Select(item => item.TryGetLocalPath())
            .Where(path => path != null)
            .Select(path => path!)
            .ToArray();
        if (paths is { Length: > 0 })
            viewModel.LoadSources(paths);
    }
}
