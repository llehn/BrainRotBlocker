using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Themes.Fluent;
using BrainRotBlocker.App.Runtime;
using System.Diagnostics;

namespace BrainRotBlocker.App.Ui;

/// <summary>The one-click installer window shown when the downloaded exe is run.</summary>
internal sealed class InstallerApp : Application
{
    private readonly InstallOptions _options;

    public InstallerApp(InstallOptions options) => _options = options;

    public InstallerApp() => throw new InvalidOperationException("InstallerApp needs options.");

    public override void Initialize()
    {
        Loc.Initialize(Loc.Auto); // pre-install: follow the Windows language
        Styles.Add(new FluentTheme());
        Styles.Add(UiTheme.BuildStyles());
        Resources.MergedDictionaries.Add(UiTheme.BuildPalette());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
            desktop.MainWindow = new InstallerWindow(_options) { Icon = ProductIcon.Create() };
        }

        base.OnFrameworkInitializationCompleted();
    }
}

internal sealed class InstallerWindow : Window
{
    private readonly InstallOptions _options;

    public InstallerWindow(InstallOptions options)
    {
        _options = options;

        Title = Loc.T("install_title");
        Width = 460;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FontFamily = new Avalonia.Media.FontFamily("Inter, $Default");
        this[!BackgroundProperty] = UiTheme.Dyn(UiTheme.AppBg);

        var icon = new Image
        {
            Source = ProductIcon.RenderBitmap(96),
            Width = 72,
            Height = 72,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        var install = UiTheme.Primary(Loc.T("install"));
        install.Click += (_, _) => DoInstall();
        var cancel = UiTheme.Ghost(Loc.T("not_now"));
        cancel.Click += (_, _) => Close();

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancel, install },
        };

        var title = UiTheme.H1(Loc.T("install_title"));
        title.HorizontalAlignment = HorizontalAlignment.Center;

        var body = UiTheme.Muted(Loc.T("install_body"));
        body.HorizontalAlignment = HorizontalAlignment.Center;
        body.TextAlignment = Avalonia.Media.TextAlignment.Center;
        body.MaxWidth = 380;

        Content = new StackPanel
        {
            Margin = new Thickness(28, 26, 28, 22),
            Spacing = 16,
            Children = { icon, title, body, actions },
        };
    }

    private void DoInstall()
    {
        try
        {
            var installer = new Installer(_options);
            installer.Install();
            Process.Start(new ProcessStartInfo(installer.InstalledExePath) { UseShellExecute = true });
            Close();
        }
        catch (Exception ex)
        {
            NativeMethods.MessageBoxW(IntPtr.Zero, ex.Message, Loc.T("install_failed"), NativeMethods.MB_ICONERROR);
        }
    }
}
