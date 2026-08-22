using Avalonia.Controls;
using Avalonia.VisualTree;
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
        ReportPage.DataContext = graph.Report;
        NamingPage.DataContext = graph.Naming;
        DrivePage.DataContext = graph.DrivePage;
        AdvancedPage.DataContext = graph.Advanced;
        ExplorePage.DataContext = graph.Explore;
        BuildStrip();
        // SLICE-015 gate: attach the soft-body behaviour to every Button in the
        // tree once it is built, and to any that appear later (dialog content,
        // items templates). No-op unless CUETOOLS_SOFTBODY=1.
        if (Controls.SoftBodyKey.Enabled)
            AttachSoftBodyKeys();
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

    private async void OnThemeToggleClick(object? sender, RoutedEventArgs e)
    {
        bool goingDark = _theme.Current == AppTheme.Light;
        await ThemeCrossfade.Run(Shell, ThemeFadeOverlay, goingDark, () =>
        {
            _theme.Toggle();
            UpdateToggleText();
            RestyleNavs();
        });
    }

    /// <summary>Startup navigation for the --convert launch flag.</summary>
    public void ShowConvertPage() => ShowPage(ConvertPage, ConvertNav);

    /// <summary>Startup navigation for the --queue launch flag.</summary>
    public void ShowQueuePage() => ShowPage(QueuePage, QueueNav);

    public void ShowSettingsPage() => ShowPage(SettingsPage, SettingsNav);

    public void ShowReportPage() => ShowPage(ReportPage, ReportNav);

    private void OnNamingNavPressed(object? sender, PointerPressedEventArgs e)
        => ShowPage(NamingPage, NamingNav);

    private void OnDriveNavPressed(object? sender, PointerPressedEventArgs e)
        => ShowPage(DrivePage, DriveNav);

    private void OnAdvancedNavPressed(object? sender, PointerPressedEventArgs e)
        => ShowPage(AdvancedPage, AdvancedNav);

    private void OnExploreNavPressed(object? sender, PointerPressedEventArgs e)
        => ShowPage(ExplorePage, ExploreNav);

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

    private void OnReportNavPressed(object? sender, PointerPressedEventArgs e)
        => ShowPage(ReportPage, ReportNav);

    private Border? _activeNav;

    private void ShowPage(Control page, Border nav)
    {
        _activeNav = nav;
        foreach (Control candidate in new Control[] { VerifyPage, ConvertPage, QueuePage, RipPage, SettingsPage, ReportPage, NamingPage, DrivePage, AdvancedPage, ExplorePage })
            candidate.IsVisible = ReferenceEquals(candidate, page);
        RestyleNavs();
        RefreshStripActives();
    }

    /// <summary>StyleNav assigns brushes resolved at call time, so a theme flip left the
    /// active card wearing the OLD theme's Face brush: a black, unreadable box on the
    /// light page (owner-reported from a live walkthrough screenshot). Restyled on every
    /// toggle with the new theme's own brushes.</summary>
    private void RestyleNavs()
    {
        foreach (Border candidate in new[] { VerifyNav, ConvertNav, QueueNav, RipNav, SettingsNav, ReportNav, NamingNav, DriveNav, AdvancedNav, ExploreNav })
            StyleNav(candidate, ReferenceEquals(candidate, _activeNav));
        foreach (var (key, nav) in _stripKeys)
        {
            key.Restyle();
            key.SetActive(ReferenceEquals(nav, _activeNav));
        }
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

    // ---- SLICE-013: the collapsed rail's icon strip and the layout breakpoints ----

    private Controls.RailLayout _railLayout = Controls.RailLayout.Full;
    private readonly List<(Controls.RailStripKey Key, Border Nav)> _stripKeys = new();

    /// <summary>The strip mirrors the card rail: same pages, same visual order,
    /// group gaps where the section headers sit. Keys pair with their nav card so
    /// activation and theme restyles stay one source of truth.</summary>
    private void BuildStrip()
    {
        (Avalonia.Media.Geometry Glyph, string Name, Border Nav, Control Page)[] rows =
        {
            (CUETools.Linux.App.Theme.RailIcons.Rip, "Rip", RipNav, RipPage),
            (CUETools.Linux.App.Theme.RailIcons.Verify, "Verify & Repair", VerifyNav, VerifyPage),
            (CUETools.Linux.App.Theme.RailIcons.Convert, "Convert", ConvertNav, ConvertPage),
            (CUETools.Linux.App.Theme.RailIcons.Queue, "Queue", QueueNav, QueuePage),
            (CUETools.Linux.App.Theme.RailIcons.Report, "Report", ReportNav, ReportPage),
            (CUETools.Linux.App.Theme.RailIcons.Naming, "Naming", NamingNav, NamingPage),
            (CUETools.Linux.App.Theme.RailIcons.Drive, "Drive & Read", DriveNav, DrivePage),
            (CUETools.Linux.App.Theme.RailIcons.Settings, "Settings", SettingsNav, SettingsPage),
            (CUETools.Linux.App.Theme.RailIcons.Advanced, "Advanced", AdvancedNav, AdvancedPage),
            (CUETools.Linux.App.Theme.RailIcons.Explore, "How a CD Works", ExploreNav, ExplorePage),
        };
        for (int i = 0; i < rows.Length; i++)
        {
            var (glyph, name, nav, page) = rows[i];
            var key = new Controls.RailStripKey(glyph, name);
            key.PointerPressed += (_, _) => ShowPage(page, nav);
            _stripKeys.Add((key, nav));
            // group gaps where the full rail draws WORK / SESSION / LEARN
            if (i is 3 or 9)
                StripPanel.Children.Add(new Border { Height = 10 });
            StripPanel.Children.Add(key);
            key.Restyle();
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        ApplyRailLayout(e.NewSize.Width);
    }

    /// <summary>D-076's two breakpoints: full rail at 1140 and up, icon strip
    /// below, and under 860 the floor - the page area holds its 860-wide layout
    /// and scrolls horizontally instead of clipping.</summary>
    private void ApplyRailLayout(double width)
    {
        Controls.RailLayout layout = Controls.RailBreakpoints.For(width);
        if (layout == _railLayout)
            return;
        _railLayout = layout;

        bool full = layout == Controls.RailLayout.Full;
        RailGrid.ColumnDefinitions[0].Width = new GridLength(full ? 214 : 56);
        FullRail.IsVisible = full;
        StripRail.IsVisible = !full;
        SettingsPage.SetCompact(!full);
        NamingPage.SetCompact(!full);

        bool floor = layout == Controls.RailLayout.Floor;
        PageScroll.HorizontalScrollBarVisibility = floor
            ? Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
            : Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled;
        PageHost.Width = floor ? Controls.RailBreakpoints.FloorBelow : double.NaN;

        RefreshStripActives();
    }

    private void RefreshStripActives()
    {
        foreach (var (key, nav) in _stripKeys)
            key.SetActive(ReferenceEquals(nav, _activeNav));
    }

    internal Controls.RailLayout RailLayoutForTest => _railLayout;

    private void AttachSoftBodyKeys()
    {
        void Sweep()
        {
            foreach (Avalonia.Visual v in this.GetVisualDescendants())
                if (v is Button b && b.GetValue(SoftBodyAttachedProperty) is not true)
                {
                    b.SetValue(SoftBodyAttachedProperty, true);
                    Controls.SoftBodyKey.Attach(b);
                }
        }

        Sweep();
        // pages are built lazily and dialogs arrive later, so re-sweep on every
        // page change rather than assuming one pass caught everything
        LayoutUpdated += (_, _) => Sweep();
    }

    private static readonly Avalonia.AttachedProperty<bool> SoftBodyAttachedProperty =
        Avalonia.AvaloniaProperty.RegisterAttached<MainWindow, Button, bool>("SoftBodyAttached");
}