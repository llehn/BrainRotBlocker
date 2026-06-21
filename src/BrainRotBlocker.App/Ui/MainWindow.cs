using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using BrainRotBlocker.App.Runtime;
using BrainRotBlocker.Core.Accounting;
using System.Threading.Tasks;

namespace BrainRotBlocker.App.Ui;

internal sealed class MainWindow : Window
{
    private enum Page { Home, Edit, Settings, Strict }

    private static readonly string[] DayNames = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
    private static readonly DayOfWeek[] DayOrder =
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
    };

    private readonly EnforcementController _controller;
    private readonly UiSettingsStore _settings;
    private readonly Action<ThemePreference> _applyTheme;

    private readonly ContentControl _host = new();
    private EditableConfiguration _editable;
    private Page _page = Page.Home;
    private bool _strictActive;

    // Editing context
    private EditableConfiguration.EditableRule? _editingRule;
    private int _editingIndex = -1;
    private StackPanel? _editSitesPanel;

    // Home live elements (rebuilt per visit)
    private readonly Dictionary<string, Action<RuleSnapshot?>> _liveUpdaters = new(StringComparer.Ordinal);
    private Border? _statusDot;
    private TextBlock? _statusText;
    private PillButton? _strictButton;

    public MainWindow(EnforcementController controller, UiSettingsStore settings, Action<ThemePreference> applyTheme)
    {
        _controller = controller;
        _settings = settings;
        _applyTheme = applyTheme;
        _editable = controller.GetEditableConfiguration();

        Title = "BrainRotBlocker";
        Width = 980;
        Height = 680;
        MinWidth = 760;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FontFamily = new FontFamily("Inter, $Default");
        this[!BackgroundProperty] = UiTheme.Dyn(UiTheme.AppBg);
        Content = _host;

        _strictActive = _controller.Status.StrictMode.IsActive;
        if (_strictActive)
        {
            NavigateStrict();
        }
        else
        {
            NavigateHome();
        }

        _controller.StatusChanged += OnStatusChanged;
        ApplyStatus(_controller.Status);
    }

    public void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    // ---------- Shared chrome ----------

    private Control TopBar(Control left, Control? right = null)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(22, 14, 18, 14),
            VerticalAlignment = VerticalAlignment.Center,
        };
        left.VerticalAlignment = VerticalAlignment.Center;
        left.HorizontalAlignment = HorizontalAlignment.Left;
        grid.Children.Add(left);
        if (right is not null)
        {
            right.VerticalAlignment = VerticalAlignment.Center;
            right.HorizontalAlignment = HorizontalAlignment.Right;
            Grid.SetColumn(right, 1);
            grid.Children.Add(right);
        }

        return new Border
        {
            Child = grid,
            BorderThickness = new Thickness(0, 0, 0, 1),
            [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.Surface),
            [!Border.BorderBrushProperty] = UiTheme.Dyn(UiTheme.Border_),
        };
    }

    private static Control Section(string label, Control body)
    {
        var stack = new StackPanel { Spacing = 10 };
        stack.Children.Add(UiTheme.SectionLabel(label));
        stack.Children.Add(body);
        return stack;
    }

    private static StackPanel HStack(double spacing, params Control[] children)
    {
        var s = new StackPanel { Orientation = Orientation.Horizontal, Spacing = spacing };
        foreach (Control c in children)
        {
            c.VerticalAlignment = VerticalAlignment.Center;
            s.Children.Add(c);
        }

        return s;
    }

    // ---------- Navigation ----------

    private void NavigateHome()
    {
        _page = Page.Home;
        _host.Content = BuildHome();
        ApplyStatus(_controller.Status);
    }

    private void NavigateEdit(EditableConfiguration.EditableRule rule, int index)
    {
        _page = Page.Edit;
        _editingRule = rule;
        _editingIndex = index;
        _host.Content = BuildEdit(rule);
    }

    private void NavigateSettings()
    {
        _page = Page.Settings;
        _host.Content = BuildSettings();
    }

    private void NavigateStrict()
    {
        _page = Page.Strict;
        _host.Content = BuildStrict();
    }

    // ---------- Home ----------

    private Control BuildHome()
    {
        _liveUpdaters.Clear();

        _statusDot = UiTheme.Dot(UiTheme.Success);
        _statusText = UiTheme.Muted("Protection active");
        var statusChip = new Border
        {
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(11, 5),
            [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.SurfaceAlt),
            Child = HStack(7, _statusDot, _statusText),
        };

        _strictButton = UiTheme.Ghost("Strict mode");
        _strictButton.Click += (_, _) => NavigateStrict();

        var settings = UiTheme.Icon("⚙", "Settings");
        settings.Click += (_, _) => NavigateSettings();

        Control header = TopBar(
            UiTheme.H1("BrainRotBlocker"),
            HStack(10, statusChip, _strictButton, settings));

        var wrap = new WrapPanel { Orientation = Orientation.Horizontal };
        for (int i = 0; i < _editable.Rules.Count; i++)
        {
            wrap.Children.Add(BuildRuleCard(_editable.Rules[i], i));
        }

        wrap.Children.Add(BuildAddTile());

        var body = new ScrollViewer
        {
            Padding = new Thickness(22, 20, 14, 20),
            Content = wrap,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };

        var root = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);
        root.Children.Add(body);
        return root;
    }

    private Control BuildRuleCard(EditableConfiguration.EditableRule rule, int index)
    {
        var dot = UiTheme.Dot(UiTheme.TextSecondary);
        var live = UiTheme.Muted("—");
        _liveUpdaters[rule.Id] = snapshot => UpdateCardLive(dot, live, rule, snapshot);

        var top = new StackPanel { Spacing = 8 };
        var name = UiTheme.H2(string.IsNullOrWhiteSpace(rule.Name) ? "Untitled rule" : rule.Name);
        top.Children.Add(name);
        top.Children.Add(UiTheme.Muted(ConditionSummary(rule)));
        top.Children.Add(UiTheme.Muted(SitesSummary(rule)));

        Control footer = HStack(7, dot, live);
        DockPanel.SetDock(footer, Dock.Bottom);

        var content = new DockPanel { LastChildFill = true };
        content.Children.Add(footer);
        content.Children.Add(top);

        Border card = UiTheme.Card(content);
        card.Height = 150;

        var grid = new Grid { Width = 296, Margin = new Thickness(0, 0, 16, 16) };
        grid.Children.Add(card);

        if (!_strictActive)
        {
            var overlay = new Border { Background = Brushes.Transparent, Cursor = new Cursor(StandardCursorType.Hand) };
            overlay.AddHandler(Gestures.TappedEvent, (_, _) => NavigateEdit(rule.Clone(), index));
            grid.Children.Add(overlay);
        }

        return grid;
    }

    private Control BuildAddTile()
    {
        var label = UiTheme.H2("+  New rule");
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label[!TextBlock.ForegroundProperty] = UiTheme.Dyn(UiTheme.Accent);

        var card = new Border
        {
            Height = 150,
            CornerRadius = new CornerRadius(14),
            BorderThickness = new Thickness(1.5),
            Child = label,
            Cursor = new Cursor(StandardCursorType.Hand),
            [!Border.BorderBrushProperty] = UiTheme.Dyn(UiTheme.Accent),
        };
        card.AddHandler(Gestures.TappedEvent, (_, _) => NavigateEdit(NewRule(), -1));

        return new Grid { Width = 296, Margin = new Thickness(0, 0, 16, 16), Children = { card } };
    }

    private void UpdateCardLive(Border dot, TextBlock text, EditableConfiguration.EditableRule rule, RuleSnapshot? s)
    {
        string key;
        string label;
        if (s is null || !s.IsActive)
        {
            key = UiTheme.TextSecondary;
            label = rule.AllDay ? "Idle" : "Off-hours";
        }
        else if (s.IsBlocking)
        {
            key = UiTheme.Danger;
            label = s.BlocksCompletely
                ? (s.ActiveWindowEndsAt is { } e ? $"Blocked until {e.ToLocalTime():HH:mm}" : "Blocked")
                : (s.HourResetsAt is { } r ? $"Used up · {r.ToLocalTime():HH:mm}" : "Used up");
        }
        else
        {
            key = s.Remaining.TotalMinutes <= 1 ? UiTheme.Warn : UiTheme.Success;
            label = $"{FormatSpan(s.Remaining)} left";
        }

        dot[!Border.BackgroundProperty] = UiTheme.Dyn(key);
        text.Text = label;
    }

    // ---------- Edit ----------

    private Control BuildEdit(EditableConfiguration.EditableRule rule)
    {
        var back = UiTheme.Ghost("←  Back");
        back.Click += (_, _) => NavigateHome();
        var cancel = UiTheme.Ghost("Cancel");
        cancel.Click += (_, _) => NavigateHome();
        var save = UiTheme.Primary("Save");
        save.Click += async (_, _) => await SaveEditingRule();

        Control header = TopBar(back, HStack(10, cancel, save));

        var nameBox = new TextBox { Text = rule.Name, Watermark = "Rule name", FontSize = 15, Width = 360, HorizontalAlignment = HorizontalAlignment.Left };
        nameBox.TextChanged += (_, _) => rule.Name = nameBox.Text ?? "";

        _editSitesPanel = new StackPanel { Spacing = 8 };
        RebuildEditSites(rule);
        var addSite = UiTheme.Ghost("+  Add site");
        addSite.HorizontalAlignment = HorizontalAlignment.Left;
        addSite.Click += (_, _) =>
        {
            rule.Sites.Add(new EditableConfiguration.EditableSite { Label = "", Url = "", IncludeSubpaths = true });
            RebuildEditSites(rule);
        };
        var what = new StackPanel { Spacing = 12, Children = { _editSitesPanel, addSite } };

        // When | What side by side so the whole rule fits without scrolling.
        var columns = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
        Control whenCol = Section("When", BuildWhenEditor(rule));
        Control whatCol = Section("What to block", what);
        whatCol.Margin = new Thickness(24, 0, 0, 0);
        Grid.SetColumn(whatCol, 1);
        columns.Children.Add(whenCol);
        columns.Children.Add(whatCol);

        var body = new StackPanel { Spacing = 22 };
        body.Children.Add(Section("Name", nameBox));
        body.Children.Add(columns);

        var scroll = new ScrollViewer
        {
            Padding = new Thickness(24, 22, 24, 16),
            Content = new Border { Child = body, MaxWidth = 900, HorizontalAlignment = HorizontalAlignment.Left },
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };

        var root = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);

        // Deletion lives in the detail view (not the overview): a pinned footer so
        // it's always reachable. Only for an existing rule.
        if (_editingIndex >= 0)
        {
            int indexToDelete = _editingIndex;
            var delete = UiTheme.Ghost("Delete this rule");
            delete.HorizontalAlignment = HorizontalAlignment.Left;
            delete.Click += async (_, _) => await DeleteRule(indexToDelete);

            var footer = new Border
            {
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(24, 12, 24, 12),
                Child = delete,
                [!Border.BorderBrushProperty] = UiTheme.Dyn(UiTheme.Border_),
            };
            DockPanel.SetDock(footer, Dock.Bottom);
            root.Children.Add(footer);
        }

        root.Children.Add(scroll);
        return root;
    }

    private Control BuildWhenEditor(EditableConfiguration.EditableRule rule)
    {
        // Allowance vs block completely
        var minutes = NumberBox(rule.AllowanceMinutes, 1, 59);
        minutes.ValueChanged += (_, _) => rule.AllowanceMinutes = (int)(minutes.Value ?? 5);
        minutes.IsEnabled = !rule.BlockCompletely;

        var allowRadio = new RadioButton { GroupName = "allowance", IsChecked = !rule.BlockCompletely };
        allowRadio.Content = HStack(8, UiTheme.Body("Allow"), minutes, UiTheme.Body("minutes per hour"));
        var blockRadio = new RadioButton { GroupName = "allowance", Content = "Block completely", IsChecked = rule.BlockCompletely, Margin = new Thickness(0, 6, 0, 0) };
        allowRadio.IsCheckedChanged += (_, _) =>
        {
            if (allowRadio.IsChecked == true)
            {
                rule.BlockCompletely = false;
                minutes.IsEnabled = true;
            }
        };
        blockRadio.IsCheckedChanged += (_, _) =>
        {
            if (blockRadio.IsChecked == true)
            {
                rule.BlockCompletely = true;
                minutes.IsEnabled = false;
            }
        };

        // Active window
        var fromBox = TimeBox(rule.From, t => rule.From = t);
        var toBox = TimeBox(rule.To, t => rule.To = t);
        var betweenRow = HStack(8, UiTheme.Body("Only between"), fromBox, UiTheme.Body("and"), toBox);
        void SetWindowEnabled(bool on) { fromBox.IsEnabled = on; toBox.IsEnabled = on; }
        SetWindowEnabled(!rule.AllDay);

        var allDayRadio = new RadioButton { GroupName = "active", Content = "All day", IsChecked = rule.AllDay };
        var betweenRadio = new RadioButton { GroupName = "active", IsChecked = !rule.AllDay, Content = betweenRow, Margin = new Thickness(0, 6, 0, 0) };
        allDayRadio.IsCheckedChanged += (_, _) =>
        {
            if (allDayRadio.IsChecked == true) { rule.AllDay = true; SetWindowEnabled(false); }
        };
        betweenRadio.IsCheckedChanged += (_, _) =>
        {
            if (betweenRadio.IsChecked == true) { rule.AllDay = false; SetWindowEnabled(true); }
        };

        // Days
        var days = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        for (int i = 0; i < DayOrder.Length; i++)
        {
            DayOfWeek day = DayOrder[i];
            var toggle = new ToggleButton
            {
                Content = DayNames[i],
                IsChecked = rule.Days.Contains(day),
                Width = 46,
                Padding = new Thickness(0, 6),
                FontSize = 11.5,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
            };
            toggle.IsCheckedChanged += (_, _) =>
            {
                if (toggle.IsChecked == true) rule.Days.Add(day);
                else rule.Days.Remove(day);
            };
            days.Children.Add(toggle);
        }

        var card = new StackPanel { Spacing = 16 };
        card.Children.Add(new StackPanel { Spacing = 0, Children = { allowRadio, blockRadio } });
        card.Children.Add(new Border { Height = 1, [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.Border_) });
        card.Children.Add(new StackPanel { Spacing = 0, Children = { allDayRadio, betweenRadio } });
        card.Children.Add(new Border { Height = 1, [!Border.BackgroundProperty] = UiTheme.Dyn(UiTheme.Border_) });
        card.Children.Add(new StackPanel { Spacing = 8, Children = { UiTheme.Muted("On these days"), days } });
        return UiTheme.Card(card);
    }

    private void RebuildEditSites(EditableConfiguration.EditableRule rule)
    {
        if (_editSitesPanel is null)
        {
            return;
        }

        _editSitesPanel.Children.Clear();
        if (rule.Sites.Count == 0)
        {
            _editSitesPanel.Children.Add(UiTheme.Muted("No sites yet — add the address of a page you want to limit."));
        }

        foreach (EditableConfiguration.EditableSite site in rule.Sites)
        {
            _editSitesPanel.Children.Add(BuildSiteRow(rule, site));
        }
    }

    private Control BuildSiteRow(EditableConfiguration.EditableRule rule, EditableConfiguration.EditableSite site)
    {
        var label = new TextBox { Text = site.Label, Watermark = "Label", Width = 116 };
        label.TextChanged += (_, _) => site.Label = label.Text ?? "";

        var url = new TextBox { Text = site.Url, Watermark = "e.g. instagram.com/reels", MinWidth = 70 };
        url.Margin = new Thickness(8, 0, 10, 0);
        url.TextChanged += (_, _) => site.Url = url.Text ?? "";

        var subpaths = new CheckBox
        {
            Content = "Subpages",
            IsChecked = site.IncludeSubpaths,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
        };
        ToolTip.SetTip(subpaths, "Also block pages underneath this address.");
        subpaths.IsCheckedChanged += (_, _) => site.IncludeSubpaths = subpaths.IsChecked == true;

        PillButton del = UiTheme.Icon("✕", "Remove site");
        del.Click += (_, _) => { rule.Sites.Remove(site); RebuildEditSites(rule); };

        var row = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(label, Dock.Left);
        DockPanel.SetDock(del, Dock.Right);
        DockPanel.SetDock(subpaths, Dock.Right);
        row.Children.Add(label);
        row.Children.Add(del);
        row.Children.Add(subpaths);
        row.Children.Add(url);
        return row;
    }

    private async Task SaveEditingRule()
    {
        if (_editingRule is null)
        {
            return;
        }

        EditableConfiguration candidate = EditableConfiguration.FromJson(_editable.ToJson());
        if (_editingIndex >= 0 && _editingIndex < candidate.Rules.Count)
        {
            candidate.Rules[_editingIndex] = _editingRule;
        }
        else
        {
            candidate.Rules.Add(_editingRule);
        }

        if (_controller.TrySaveConfiguration(candidate, out string? error))
        {
            _editable = _controller.GetEditableConfiguration();
            NavigateHome();
            return;
        }

        await Dialogs.Message(this, "Couldn't save", error ?? "The rule is not valid.");
    }

    private async Task DeleteRule(int index)
    {
        if (index < 0 || index >= _editable.Rules.Count)
        {
            return;
        }

        if (!await Dialogs.Confirm(this, "Delete rule", $"Delete “{_editable.Rules[index].Name}”?", "Delete"))
        {
            return;
        }

        EditableConfiguration candidate = EditableConfiguration.FromJson(_editable.ToJson());
        candidate.Rules.RemoveAt(index);
        if (_controller.TrySaveConfiguration(candidate, out string? error))
        {
            _editable = _controller.GetEditableConfiguration();
            NavigateHome();
            return;
        }

        await Dialogs.Message(this, "Couldn't delete", error ?? "Could not save.");
    }

    private EditableConfiguration.EditableRule NewRule() => new()
    {
        Id = NextId(),
        Name = "New rule",
        BlockCompletely = false,
        AllowanceMinutes = 5,
        AllDay = true,
    };

    // ---------- Settings ----------

    private Control BuildSettings()
    {
        var back = UiTheme.Ghost("←  Back");
        back.Click += (_, _) => NavigateHome();
        Control header = TopBar(HStack(12, back, UiTheme.H1("Settings")));

        ThemePreference current = _settings.LoadTheme();
        var group = new StackPanel { Spacing = 4 };
        foreach (ThemePreference pref in new[] { ThemePreference.System, ThemePreference.Light, ThemePreference.Dark })
        {
            ThemePreference captured = pref;
            var radio = new RadioButton
            {
                GroupName = "theme",
                Content = pref switch
                {
                    ThemePreference.Light => "Light",
                    ThemePreference.Dark => "Dark",
                    _ => "Follow system",
                },
                IsChecked = pref == current,
            };
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true)
                {
                    _settings.SaveTheme(captured);
                    _applyTheme(captured);
                }
            };
            group.Children.Add(radio);
        }

        var body = new StackPanel { Spacing = 22, Margin = new Thickness(24, 22, 24, 24) };
        body.Children.Add(Section("Appearance", UiTheme.Card(group)));

        var root = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);
        root.Children.Add(body);
        return root;
    }

    // ---------- Strict mode (minimal for now) ----------

    private Control BuildStrict()
    {
        var back = UiTheme.Ghost("←  Back");
        back.Click += (_, _) => NavigateHome();
        Control header = TopBar(HStack(12, back, UiTheme.H1("Strict mode")));

        StrictModeSnapshot strict = _controller.Status.StrictMode;
        var body = new StackPanel { Spacing = 16, Margin = new Thickness(24, 22, 24, 24), MaxWidth = 560, HorizontalAlignment = HorizontalAlignment.Left };

        if (strict.IsActive)
        {
            body.Children.Add(UiTheme.H2("Strict mode is on"));
            body.Children.Add(UiTheme.Body($"Locked until {strict.ActiveUntilLocal:dddd HH:mm} · {FormatSpan(strict.Remaining)} left."));
            body.Children.Add(UiTheme.Muted("Your rules are locked until strict mode ends. You can still add new rules."));
        }
        else
        {
            body.Children.Add(UiTheme.Body("Lock your rules and keep enforcement on for a set time. No edits and no exit until it ends."));

            var amount = NumberBox(2, 1, 999);
            amount.Width = 90;
            var unit = new ComboBox { Width = 130, ItemsSource = new[] { "minutes", "hours", "days" }, SelectedIndex = 1 };
            var lockBtn = UiTheme.Primary("Lock in strict mode");
            lockBtn.Click += async (_, _) =>
            {
                int n = (int)(amount.Value ?? 1);
                TimeSpan d = unit.SelectedIndex switch
                {
                    0 => TimeSpan.FromMinutes(n),
                    2 => TimeSpan.FromDays(n),
                    _ => TimeSpan.FromHours(n),
                };
                if (await Dialogs.StrictConfirm(this, n, (string)unit.SelectedItem!))
                {
                    _controller.ActivateStrictMode(d);
                    NavigateStrict();
                }
            };

            body.Children.Add(UiTheme.Card(new StackPanel
            {
                Spacing = 14,
                Children = { HStack(10, UiTheme.Body("For"), amount, unit), lockBtn },
            }));
        }

        var root = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);
        root.Children.Add(body);
        return root;
    }

    // ---------- Status ----------

    private void OnStatusChanged(object? sender, AppStatus status) => Dispatcher.UIThread.Post(() => ApplyStatus(status));

    private void ApplyStatus(AppStatus status)
    {
        bool wasStrict = _strictActive;
        _strictActive = status.StrictMode.IsActive;

        // Strict mode is the landing screen while active.
        if (_strictActive && !wasStrict && _page != Page.Strict)
        {
            NavigateStrict();
            return;
        }

        if (_page == Page.Home)
        {
            if (_statusDot is not null && _statusText is not null)
            {
                bool healthy = status.LastError is null;
                _statusDot[!Border.BackgroundProperty] = UiTheme.Dyn(healthy ? UiTheme.Success : UiTheme.Warn);
                _statusText.Text = healthy ? "Protection active" : "Needs attention";
            }

            if (_strictButton is not null)
            {
                _strictButton.Text = _strictActive
                    ? $"Strict · {FormatSpan(status.StrictMode.Remaining)} left"
                    : "Strict mode";
            }

            var byId = status.Rules.ToDictionary(r => r.RuleId, r => r, StringComparer.Ordinal);
            foreach ((string id, Action<RuleSnapshot?> update) in _liveUpdaters)
            {
                update(byId.TryGetValue(id, out RuleSnapshot? s) ? s : null);
            }
        }
    }

    // ---------- Inputs & dialogs ----------

    private static NumericUpDown NumberBox(int value, int min, int max) => new()
    {
        Value = value,
        Minimum = min,
        Maximum = max,
        Increment = 1,
        FormatString = "0",
        ShowButtonSpinner = false,
        Width = 70,
        VerticalAlignment = VerticalAlignment.Center,
    };

    private static TextBox TimeBox(TimeOnly value, Action<TimeOnly> onChanged)
    {
        var box = new TextBox { Text = value.ToString("HH:mm"), Width = 70, Watermark = "23:00", VerticalAlignment = VerticalAlignment.Center };
        box.LostFocus += (_, _) =>
        {
            if (TimeOnly.TryParse(box.Text, out TimeOnly t))
            {
                onChanged(t);
                box.Text = t.ToString("HH:mm");
            }
            else
            {
                box.Text = value.ToString("HH:mm");
            }
        };
        return box;
    }


    // ---------- Helpers ----------

    private string ConditionSummary(EditableConfiguration.EditableRule rule)
    {
        string when = rule.BlockCompletely ? "Blocked" : $"{rule.AllowanceMinutes} min / hour";
        string active = rule.AllDay ? "all day" : $"{rule.From:HH\\:mm}–{rule.To:HH\\:mm}";
        return $"{when} · {active} · {DaysSummary(rule.Days)}";
    }

    private static string SitesSummary(EditableConfiguration.EditableRule rule)
    {
        if (rule.Sites.Count == 0)
        {
            return "No sites yet";
        }

        var labels = rule.Sites
            .Select(s => string.IsNullOrWhiteSpace(s.Label) ? s.Url : s.Label)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
        string head = string.Join(", ", labels.Take(2));
        return labels.Count > 2 ? $"{head} +{labels.Count - 2} more" : head;
    }

    private static string DaysSummary(ICollection<DayOfWeek> days)
    {
        if (days.Count == 7)
        {
            return "every day";
        }

        bool weekdays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }.All(days.Contains) && days.Count == 5;
        if (weekdays)
        {
            return "weekdays";
        }

        if (days.Count == 2 && days.Contains(DayOfWeek.Saturday) && days.Contains(DayOfWeek.Sunday))
        {
            return "weekends";
        }

        return string.Join(", ", DayOrder.Where(days.Contains).Select(d => DayNames[Array.IndexOf(DayOrder, d)]));
    }

    private static string FormatSpan(TimeSpan span)
    {
        if (span.TotalHours >= 1)
        {
            return $"{(int)span.TotalHours}h {span.Minutes}m";
        }

        if (span.TotalMinutes >= 1)
        {
            return $"{span.Minutes}m {span.Seconds}s";
        }

        return $"{Math.Max(0, span.Seconds)}s";
    }

    private string NextId()
    {
        var existing = _editable.Rules.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        for (int i = 1; ; i++)
        {
            string candidate = $"rule-{i}";
            if (!existing.Contains(candidate))
            {
                return candidate;
            }
        }
    }
}
