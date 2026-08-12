using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;

namespace CUETools.Linux.App;

public class App : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new Window
            {
                Width = 960,
                Height = 600,
                Title = "CUETools Linux",
                Content = new TextBlock
                {
                    Text = "CUETools Linux (scaffold): the Verify workspace arrives with milestone M4.",
                    Margin = new Thickness(24),
                },
            };
            window.Opened += (_, _) =>
            {
                // --smoke: prove the app reaches a visible window, then exit
                // (used by CI and by startup measurements; the stopwatch is
                // started in Main so the number covers the whole launch).
                Console.WriteLine($"startup-to-window-ms={Program.Startup.ElapsedMilliseconds}");
                if (desktop.Args is ["--smoke", ..])
                {
                    desktop.Shutdown();
                }
            };
            desktop.MainWindow = window;
        }
        base.OnFrameworkInitializationCompleted();
    }
}

public static class Program
{
    internal static readonly Stopwatch Startup = Stopwatch.StartNew();

    [STAThread]
    public static void Main(string[] args) => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .StartWithClassicDesktopLifetime(args);
}
