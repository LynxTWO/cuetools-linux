using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using CUETools.Linux.App.Services;
using CUETools.Wpf.Services;
using CUETools.Wpf.Services.Artwork;

namespace CUETools.Linux.App.Views;

/// <summary>
/// The artwork chooser for the rip page: every candidate the metadata
/// lookup found, with provider, dimensions, match reason, and a lazily
/// downloaded thumbnail. Returns the chosen candidate, or null when the
/// user cancels (no change). Built in code like the app's other small
/// dialogs.
/// </summary>
public sealed class ArtworkBrowserWindow : Window
{
    private readonly IAlbumArtService _art;
    private readonly IArtworkPreviewFactory _previews;
    private readonly ListBox _list;
    private readonly CancellationTokenSource _cts = new();

    public ArtworkBrowserWindow(
        ObservableCollection<ArtworkCandidate> candidates,
        string? selectedId,
        IAlbumArtService art,
        IArtworkPreviewFactory previews)
    {
        _art = art;
        _previews = previews;
        Title = "Choose artwork";
        Width = 640;
        Height = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var ordered = candidates.OrderBy(c => c, new ArtworkCandidateComparer()).ToList();
        _list = new ListBox
        {
            ItemsSource = ordered.Select(BuildRow).ToList(),
            SelectedIndex = Math.Max(0, ordered.FindIndex(c => c.CandidateId == selectedId)),
        };

        var use = new Button { Content = "Use this artwork", IsDefault = true };
        use.Click += (_, _) =>
        {
            int index = _list.SelectedIndex;
            Close(index >= 0 && index < ordered.Count ? ordered[index] : null);
        };
        var cancel = new Button { Content = "Cancel", IsCancel = true };
        cancel.Click += (_, _) => Close(null);

        Content = new DockPanel
        {
            Margin = new Thickness(14),
            Children =
            {
                Dock(new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0),
                    Children = { cancel, use },
                }, Avalonia.Controls.Dock.Bottom),
                new ScrollViewer { Content = _list },
            },
        };

        Closed += (_, _) => _cts.Cancel();
    }

    private static Control Dock(Control control, Dock dock)
    {
        DockPanel.SetDock(control, dock);
        return control;
    }

    private Control BuildRow(ArtworkCandidate candidate)
    {
        var thumb = new Border
        {
            Width = 72,
            Height = 72,
            Margin = new Thickness(0, 0, 10, 0),
            Background = Brushes.Transparent,
        };
        LoadThumbAsync(candidate, thumb);

        string headline = candidate.Provider +
            (candidate.IsApproved ? "  (approved)" : candidate.IsPrimary ? "  (primary)" : "");
        string detail = string.Join("   ", new[]
        {
            candidate.ArtworkType,
            candidate.DimensionsText,
            candidate.SizeText,
            candidate.MatchText,
        }.Where(part => !string.IsNullOrWhiteSpace(part)));

        return new DockPanel
        {
            Margin = new Thickness(2, 4),
            Children =
            {
                Dock(thumb, Avalonia.Controls.Dock.Left),
                new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new TextBlock { Text = headline, FontSize = 13 },
                        new TextBlock
                        {
                            Text = detail,
                            FontSize = 11,
                            Opacity = 0.7,
                        },
                        new TextBlock
                        {
                            Text = candidate.MatchReason,
                            FontSize = 10.5,
                            Opacity = 0.55,
                            TextWrapping = TextWrapping.Wrap,
                        },
                    },
                },
            },
        };
    }

    private async void LoadThumbAsync(ArtworkCandidate candidate, Border host)
    {
        try
        {
            AlbumArt? art = await Task.Run(
                () => _art.DownloadAsync(candidate, thumbnail: true, _cts.Token));
            if (art == null || _cts.IsCancellationRequested) return;
            object? image = _previews.CreatePreview(art.Bytes, 144);
            if (image == null) return;
            await Dispatcher.UIThread.InvokeAsync(() =>
                host.Child = new Image
                {
                    Source = image as Avalonia.Media.Imaging.Bitmap,
                    Stretch = Stretch.Uniform,
                });
        }
        catch
        {
            // A missing thumbnail is cosmetic; the row's text still identifies
            // the candidate.
        }
    }
}
