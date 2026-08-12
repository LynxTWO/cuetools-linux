using Avalonia;
using Avalonia.Styling;

namespace CUETools.Linux.App.Services;

public enum AppTheme
{
    Dark,
    Light,
}

/// <summary>
/// Theme state and persistence, the Linux counterpart of the fork's
/// ThemeService. Application is trivial here: Avalonia's RequestedThemeVariant
/// drives the Palette.axaml theme dictionaries natively, so switching is one
/// property write instead of the WPF palette-swap mechanism. Dark is the
/// default, matching the WPF app. The preference lives under XDG config.
/// </summary>
public sealed class ThemeState
{
    private readonly string _prefPath;

    public AppTheme Current { get; private set; } = AppTheme.Dark;

    public event EventHandler? Changed;

    public ThemeState()
    {
        // Environment.SpecialFolder.ApplicationData maps to XDG_CONFIG_HOME
        // (default ~/.config) on Linux.
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "cuetools-linux");
        _prefPath = Path.Combine(dir, "theme.txt");
        try
        {
            if (File.ReadAllText(_prefPath).Trim() == "Light")
            {
                Current = AppTheme.Light;
            }
        }
        catch
        {
            // No saved preference: stay dark.
        }
    }

    public void Apply(AppTheme theme)
    {
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = theme == AppTheme.Light
                ? ThemeVariant.Light
                : ThemeVariant.Dark;
        }

        Current = theme;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_prefPath)!);
            File.WriteAllText(_prefPath, theme.ToString());
        }
        catch
        {
            // A failed preference write never blocks the theme change.
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Toggle() => Apply(Current == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
}
