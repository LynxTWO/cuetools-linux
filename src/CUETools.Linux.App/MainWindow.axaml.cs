using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using CUETools.Linux.App.Journal;
using CUETools.Linux.App.Services;
using CUETools.Linux.App.Views;
using CUETools.Wpf.ViewModels;

namespace CUETools.Linux.App;

public partial class MainWindow : Window
{
    private readonly ThemeState _theme;
    private Composition.AppGraph? _graph;

    public MainWindow(ThemeState theme, VerifyViewModel verify, ConvertViewModel convert,
        Composition.AppGraph graph)
    {
        _theme = theme;
        _graph = graph;
        InitializeComponent();
        VerifyPage.DataContext = verify;
        VerifyPage.InitEnrichment(graph.Enrichment);
        ConvertPage.DataContext = convert;
        ConvertPage.Init(graph.Config, graph.Catalog);
        QueuePage.DataContext = graph.Queue;
        SettingsPage.DataContext = graph.SettingsPage;
        RipPage.DataContext = graph.Rip;
        RipPage.Init(graph.Config, graph.Catalog, graph.Art);
        UpdateToggleText();
        RefreshEnrichPending();
    }

    /// <summary>The D-010 enrichment lane's surface: offline lookups that
    /// journaled show up as a rail card counting the albums ready to review;
    /// proposals are generated fresh at review time, never stored.</summary>
    private void RefreshEnrichPending()
    {
        if (_graph == null) return;
        int pending = _graph.Journal.ReadPending(BackfillLane.Enrichment).Count;
        EnrichPendingNav.IsVisible = pending > 0;
        EnrichPendingTitle.Text = $"Enrichment pending ({pending})";
    }

    private async void OnEnrichPendingPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_graph == null) return;
        foreach (BackfillJournalEntry entry in
            _graph.Journal.ReadPending(BackfillLane.Enrichment))
        {
            try
            {
                if (!File.Exists(entry.SourcePath) && !Directory.Exists(entry.SourcePath))
                {
                    entry.State = BackfillState.Unresolvable;
                    entry.Reason = "the album no longer exists at its journaled path";
                    _graph.Journal.Update(entry);
                    continue;
                }
                EnrichmentProposal? proposal = await Task.Run(
                    () => _graph.Enrichment.Propose(entry.SourcePath));
                if (proposal is { HasChanges: true })
                {
                    var dialog = new EnrichmentDialog(proposal);
                    bool approved = await dialog.ShowDialog<bool>(this);
                    if (approved)
                        await Task.Run(() => _graph.Enrichment.Apply(proposal));
                    // Seen and decided either way: the pending entry is resolved.
                    entry.State = BackfillState.Resolved;
                    entry.Reason = approved ? "applied" : "declined by the user";
                }
                else
                {
                    entry.State = BackfillState.Resolved;
                    entry.Reason = proposal == null
                        ? "no database release found"
                        : "album already matches the database";
                }
                _graph.Journal.Update(entry);
            }
            catch (EnrichmentOfflineException)
            {
                break; // still offline; entries stay pending
            }
            catch (Exception ex)
            {
                _graph.Log.Warn("enrich", "pending review failed: " + ex.GetType().Name);
            }
        }
        RefreshEnrichPending();
    }

    private void OnThemeToggleClick(object? sender, RoutedEventArgs e)
    {
        _theme.Toggle();
        UpdateToggleText();
    }

    /// <summary>Startup navigation for the --convert launch flag.</summary>
    public void ShowConvertPage() => ShowPage(ConvertPage, ConvertNav);

    /// <summary>Startup navigation for the --queue launch flag.</summary>
    public void ShowQueuePage() => ShowPage(QueuePage, QueueNav);

    public void ShowSettingsPage() => ShowPage(SettingsPage, SettingsNav);

    /// <summary>D-073: a secondary drive window never publishes shared settings, so it
    /// does not show the page at all. Removing the card beats explaining a read-only one.</summary>
    public void HideSettingsNav() => SettingsNav.IsVisible = false;

    /// <summary>Startup navigation for the --rip-page launch flag.</summary>
    public void ShowRipPage() => ShowPage(RipPage, RipNav);

    private void OnRipNavPressed(object? sender, PointerPressedEventArgs e)
        => ShowPage(RipPage, RipNav);

    private void OnVerifyNavPressed(object? sender, PointerPressedEventArgs e)
        => ShowPage(VerifyPage, VerifyNav);

    private void OnConvertNavPressed(object? sender, PointerPressedEventArgs e)
        => ShowPage(ConvertPage, ConvertNav);

    private void OnQueueNavPressed(object? sender, PointerPressedEventArgs e)
        => ShowPage(QueuePage, QueueNav);

    private void OnSettingsNavPressed(object? sender, PointerPressedEventArgs e)
        => ShowPage(SettingsPage, SettingsNav);

    private void ShowPage(Control page, Border nav)
    {
        foreach (Control candidate in new Control[] { VerifyPage, ConvertPage, QueuePage, RipPage, SettingsPage })
            candidate.IsVisible = ReferenceEquals(candidate, page);
        foreach (Border candidate in new[] { VerifyNav, ConvertNav, QueueNav, RipNav, SettingsNav })
            StyleNav(candidate, ReferenceEquals(candidate, nav));
    }

    private void StyleNav(Border nav, bool active)
    {
        nav.Background = active
            ? GetBrush("Face")
            : Brushes.Transparent;
        nav.BorderBrush = GetBrush(active ? "StatusAccent" : "Line");
    }

    private IBrush GetBrush(string key)
        => this.TryFindResource(key, ActualThemeVariant, out object? value) &&
           value is IBrush brush
            ? brush
            : Brushes.Transparent;

    private void UpdateToggleText()
        => ThemeToggle.Content = _theme.Current == AppTheme.Dark ? "Light theme" : "Dark theme";
}
