using Avalonia.Controls;
using Avalonia.Interactivity;
using CUETools.Wpf.ViewModels;

namespace CUETools.Linux.App.Views;

public partial class AdvancedView : UserControl
{
    public AdvancedView()
    {
        InitializeComponent();
    }

    private void OnSetProxyPassword(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AdvancedViewModel vm && vm.SetProxyPassword(ProxyPasswordInput.Text ?? ""))
            ProxyPasswordInput.Text = "";
    }

    private void OnClearProxyPassword(object? sender, RoutedEventArgs e)
    {
        (DataContext as AdvancedViewModel)?.ClearProxyPassword();
        ProxyPasswordInput.Text = "";
    }
}
