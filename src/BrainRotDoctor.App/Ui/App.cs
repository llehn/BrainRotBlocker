using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using BrainRotDoctor.App.Runtime;

namespace BrainRotDoctor.App.Ui;

/// <summary>The Avalonia application: theme, tray icon, and the main window.</summary>
internal sealed class App : Application
{
    private readonly EnforcementController _controller;
    private readonly UiSettingsStore _settings;
    private MainWindow? _window;

    public App(EnforcementController controller, UiSettingsStore settings)
    {
        _controller = controller;
        _settings = settings;
    }

    public override void Initialize()
    {
        Loc.Initialize(_settings.LoadLanguage());
        Styles.Add(new FluentTheme());
        Styles.Add(UiTheme.BuildStyles());
        Resources.MergedDictionaries.Add(UiTheme.BuildPalette());
        ApplyTheme(_settings.LoadTheme());
    }

    public void ApplyTheme(ThemePreference theme) => RequestedThemeVariant = theme switch
    {
        ThemePreference.Light => ThemeVariant.Light,
        ThemePreference.Dark => ThemeVariant.Dark,
        _ => ThemeVariant.Default,
    };

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            WindowIcon icon = ProductIcon.Create();
            _window = new MainWindow(_controller, _settings, ApplyTheme) { Icon = icon };

            var tray = new TrayIcon { Icon = icon, ToolTipText = "BrainRotDoctor", IsVisible = true };
            var menu = new NativeMenu();
            var open = new NativeMenuItem("Open BrainRotDoctor");
            open.Click += (_, _) => _window.ShowFromTray();
            menu.Items.Add(open);
            tray.Menu = menu;
            tray.Clicked += (_, _) => _window.ShowFromTray();

            desktop.MainWindow = _window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
