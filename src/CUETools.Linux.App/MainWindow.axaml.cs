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
        ConvertPage.DataContext = convert;
        ConvertPage.Init(graph.Config, graph.Catalog);
        UpdateToggleText();
    }

    private void OnThemeToggleClick(object? sender, RoutedEventArgs e)
    {
        _theme.Toggle();
        UpdateToggleText();
    }

    /// <summary>Startup navigation for the --convert launch flag.</summary>
    public void ShowConvertPage() => ShowPage(verify: false);

    private void OnVerifyNavPressed(object? sender, PointerPressedEventArgs e)
        => ShowPage(verify: true);

    private void OnConvertNavPressed(object? sender, PointerPressedEventArgs e)
        => ShowPage(verify: false);

    private void ShowPage(bool verify)
    {
        VerifyPage.IsVisible = verify;
        ConvertPage.IsVisible = !verify;
        StyleNav(VerifyNav, verify);
        StyleNav(ConvertNav, !verify);
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
