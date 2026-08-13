using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using CUETools.Linux.App.Services;
using CUETools.Wpf.ViewModels;

namespace CUETools.Linux.App;

public partial class MainWindow : Window
{
    private readonly ThemeState _theme;

    public MainWindow(ThemeState theme, VerifyViewModel verify, ConvertViewModel convert,
        Composition.AppGraph graph)
    {
        _theme = theme;
        InitializeComponent();
        VerifyPage.DataContext = verify;
        VerifyPage.InitEnrichment(graph.Enrichment);
        ConvertPage.DataContext = convert;
        ConvertPage.Init(graph.Config, graph.Catalog);
        QueuePage.DataContext = graph.Queue;
        UpdateToggleText();
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

    private void OnVerifyNavPressed(object? sender, PointerPressedEventArgs e)
        => ShowPage(VerifyPage, VerifyNav);

    private void OnConvertNavPressed(object? sender, PointerPressedEventArgs e)
        => ShowPage(ConvertPage, ConvertNav);

    private void OnQueueNavPressed(object? sender, PointerPressedEventArgs e)
        => ShowPage(QueuePage, QueueNav);

    private void ShowPage(Control page, Border nav)
    {
        foreach (Control candidate in new Control[] { VerifyPage, ConvertPage, QueuePage })
            candidate.IsVisible = ReferenceEquals(candidate, page);
        foreach (Border candidate in new[] { VerifyNav, ConvertNav, QueueNav })
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
