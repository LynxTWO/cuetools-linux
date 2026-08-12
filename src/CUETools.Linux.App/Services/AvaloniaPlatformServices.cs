using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CUETools.Wpf.Services;

namespace CUETools.Linux.App.Services;

/// <summary>Avalonia implementation of the app core's file/folder pickers,
/// backed by the window's StorageProvider.</summary>
public sealed class AvaloniaFileDialogService : IFileDialogService
{
    private readonly Func<Window?> _windowSource;

    public AvaloniaFileDialogService(Func<Window?> windowSource)
        => _windowSource = windowSource;

    public async Task<string[]?> PickFilesAsync(
        string title, bool multiselect, IReadOnlyList<FilePickerGroup> groups)
    {
        if (_windowSource() is not { } window) return null;
        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = multiselect,
            FileTypeFilter = groups.Select(group => new FilePickerFileType(group.Name)
            {
                Patterns = group.Extensions
                    .Select(extension => extension == "*" ? "*" : "*." + extension)
                    .ToArray(),
            }).ToArray(),
        });
        string[] paths = files
            .Select(file => file.TryGetLocalPath())
            .Where(path => path != null)
            .Select(path => path!)
            .ToArray();
        return paths.Length > 0 ? paths : null;
    }

    public async Task<string?> PickFolderAsync(string title)
    {
        if (_windowSource() is not { } window) return null;
        var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
        });
        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }
}

/// <summary>Avalonia implementation of the app core's confirmations: a small
/// modal window styled by the 2026 palette.</summary>
public sealed class AvaloniaUserPrompt : IUserPrompt
{
    private readonly Func<Window?> _windowSource;

    public AvaloniaUserPrompt(Func<Window?> windowSource)
        => _windowSource = windowSource;

    public async Task<bool> ConfirmOkCancelAsync(string message, string title)
    {
        if (_windowSource() is not { } owner) return false;

        var result = false;
        var dialog = new Window
        {
            Title = title,
            SizeToContent = SizeToContent.WidthAndHeight,
            MaxWidth = 520,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };
        var ok = new Button { Content = "OK", MinWidth = 84 };
        var cancel = new Button { Content = "Cancel", MinWidth = 84 };
        ok.Click += (_, _) => { result = true; dialog.Close(); };
        cancel.Click += (_, _) => dialog.Close();
        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(24),
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { cancel, ok },
                },
            },
        };
        await dialog.ShowDialog(owner);
        return result;
    }
}

/// <summary>Avalonia implementation of the app core's UI-thread seam.</summary>
public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public bool CheckAccess() => Dispatcher.UIThread.CheckAccess();

    public void Post(Action action) => Dispatcher.UIThread.Post(action);
}
