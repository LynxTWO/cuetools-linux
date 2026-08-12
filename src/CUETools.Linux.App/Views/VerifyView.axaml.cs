using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using CUETools.Wpf.ViewModels;

namespace CUETools.Linux.App.Views;

public partial class VerifyView : UserControl
{
    public VerifyView()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
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
