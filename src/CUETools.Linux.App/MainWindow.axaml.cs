using Avalonia.Controls;
using Avalonia.Interactivity;
using CUETools.Linux.App.Services;

namespace CUETools.Linux.App;

public partial class MainWindow : Window
{
    private readonly ThemeState _theme;

    public MainWindow(ThemeState theme)
    {
        _theme = theme;
        InitializeComponent();
        UpdateToggleText();
    }

    private void OnThemeToggleClick(object? sender, RoutedEventArgs e)
    {
        _theme.Toggle();
        UpdateToggleText();
    }

    private void UpdateToggleText()
        => ThemeToggle.Content = _theme.Current == AppTheme.Dark ? "Light theme" : "Dark theme";
}
