using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using System.Threading.Tasks;

namespace BrainRotBlocker.App.Ui;

/// <summary>Shared modal dialogs styled with the app palette.</summary>
internal static class Dialogs
{
    public static async Task Message(Window owner, string title, string message)
    {
        var ok = UiTheme.Primary("OK");
        ok.HorizontalAlignment = HorizontalAlignment.Right;
        Window dialog = Shell(owner, title, new StackPanel
        {
            Margin = new Thickness(22),
            Spacing = 18,
            Children = { UiTheme.H2(title), UiTheme.Body(message), ok },
        });
        ok.Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(owner);
    }

    public static async Task<bool> Confirm(Window owner, string title, string message, string continueText)
    {
        var result = false;
        var cancel = UiTheme.Ghost("Cancel");
        var ok = UiTheme.Primary(continueText);
        var actions = Actions(cancel, ok);
        Window dialog = Shell(owner, title, new StackPanel
        {
            Margin = new Thickness(22),
            Spacing = 18,
            Children = { UiTheme.H2(title), UiTheme.Body(message), actions },
        });
        cancel.Click += (_, _) => dialog.Close();
        ok.Click += (_, _) => { result = true; dialog.Close(); };
        await dialog.ShowDialog(owner);
        return result;
    }

    /// <summary>Strict-mode double opt-in: a checkbox gates the confirm button.</summary>
    public static async Task<bool> StrictConfirm(Window owner, int amount, string unit)
    {
        var result = false;
        var ack = new CheckBox { Content = "I understand I won't be able to edit my rules or stop until it ends." };
        var cancel = UiTheme.Ghost("Cancel");
        var ok = UiTheme.Primary("Lock in strict mode");
        ok.Opacity = 0.45;

        var panel = new StackPanel
        {
            Margin = new Thickness(22),
            Spacing = 16,
            Children =
            {
                UiTheme.H2("Lock in strict mode?"),
                UiTheme.Body($"This locks your rules for {amount} {unit}. There is no early exit — you can't edit rules or turn it off until the time is up."),
                ack,
                Actions(cancel, ok),
            },
        };

        Window dialog = Shell(owner, "Lock in strict mode?", panel, width: 460);

        bool armed = false;
        ack.IsCheckedChanged += (_, _) => { armed = ack.IsChecked == true; ok.Opacity = armed ? 1 : 0.45; };
        cancel.Click += (_, _) => dialog.Close();
        ok.Click += (_, _) => { if (armed) { result = true; dialog.Close(); } };

        await dialog.ShowDialog(owner);
        return result;
    }

    private static Control Actions(params Control[] buttons)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        foreach (Control b in buttons)
        {
            row.Children.Add(b);
        }

        return row;
    }

    private static Window Shell(Window owner, string title, Control content, double width = 420) => new()
    {
        Title = title,
        Width = width,
        SizeToContent = SizeToContent.Height,
        CanResize = false,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        FontFamily = owner.FontFamily,
        Content = content,
        [!Window.BackgroundProperty] = UiTheme.Dyn(UiTheme.AppBg),
    };
}
