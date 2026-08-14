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

/// <summary>Avalonia UI-thread timers behind the app core's timer seam.
/// Avalonia has no render-priority timer concept; the 16 ms telemetry tick
/// approximates frame alignment well enough for the live visuals.</summary>
public sealed class AvaloniaUiTimerFactory : IUiTimerFactory
{
    public IUiTimer Create(TimeSpan interval, bool renderPriority = false)
        => new AvaloniaUiTimer(interval);

    private sealed class AvaloniaUiTimer : IUiTimer
    {
        private readonly DispatcherTimer _timer = new();

        public AvaloniaUiTimer(TimeSpan interval)
        {
            _timer.Interval = interval;
            _timer.Tick += (_, _) => Tick?.Invoke(this, EventArgs.Empty);
        }

        public TimeSpan Interval
        {
            get => _timer.Interval;
            set => _timer.Interval = value;
        }

        public bool IsEnabled => _timer.IsEnabled;

        public void Start() => _timer.Start();

        public void Stop() => _timer.Stop();

        public event EventHandler? Tick;

        public void Dispose() => _timer.Stop();
    }
}

/// <summary>Artwork previews as Avalonia bitmaps, decode-bounded like the
/// WPF DecodePixelWidth path.</summary>
public sealed class AvaloniaArtworkPreviewFactory : IArtworkPreviewFactory
{
    public object? CreatePreview(byte[] bytes, int decodeWidth)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            return decodeWidth > 0
                ? Avalonia.Media.Imaging.Bitmap.DecodeToWidth(stream, decodeWidth)
                : new Avalonia.Media.Imaging.Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>This head renders the 2D read map; there is no hardware 3D disc
/// visual yet, and saying so keeps the view model's fallback honest.</summary>
public sealed class LinuxPlatformCapabilities : IPlatformCapabilities
{
    public bool HardwareAccelerated3D => false;
}

/// <summary>SkiaSharp artwork transcoding with the same contract as the WPF
/// implementation: decode failure returns null, a JPEG already within the
/// cap passes through untranscoded, larger images resize to the cap's long
/// edge and encode as JPEG at the given quality.</summary>
public sealed class SkiaImageTranscoder : IImageTranscoder
{
    public bool CanDecode(byte[] bytes)
    {
        using var codec = SkiaSharp.SKCodec.Create(new MemoryStream(bytes));
        return codec != null;
    }

    public byte[]? ResizeToJpeg(byte[] source, int maxSize, int quality)
    {
        using var codec = SkiaSharp.SKCodec.Create(new MemoryStream(source));
        if (codec == null)
            return null;
        var info = codec.Info;
        bool isJpeg = codec.EncodedFormat == SkiaSharp.SKEncodedImageFormat.Jpeg;
        if (isJpeg && info.Width <= maxSize && info.Height <= maxSize)
            return source;

        using var original = SkiaSharp.SKBitmap.Decode(source);
        if (original == null)
            return null;
        SkiaSharp.SKBitmap final = original;
        SkiaSharp.SKBitmap? resized = null;
        try
        {
            if (original.Width > maxSize || original.Height > maxSize)
            {
                double scale = Math.Min(
                    (double)maxSize / original.Width,
                    (double)maxSize / original.Height);
                var size = new SkiaSharp.SKImageInfo(
                    Math.Max(1, (int)(original.Width * scale)),
                    Math.Max(1, (int)(original.Height * scale)));
                resized = original.Resize(size, SkiaSharp.SKSamplingOptions.Default);
                if (resized != null)
                    final = resized;
            }
            using var image = SkiaSharp.SKImage.FromBitmap(final);
            using var data = image.Encode(
                SkiaSharp.SKEncodedImageFormat.Jpeg,
                Math.Clamp(quality, 1, 100));
            return data.ToArray();
        }
        finally
        {
            resized?.Dispose();
        }
    }
}
