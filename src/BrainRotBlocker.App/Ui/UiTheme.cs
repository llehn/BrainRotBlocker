using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;

namespace BrainRotBlocker.App.Ui;

/// <summary>
/// The app's visual language: a soft light/dark palette exposed as theme-aware
/// resources, plus factory helpers that build consistently-styled controls.
/// Surface and text colours are referenced with <see cref="DynamicResourceExtension"/>
/// so everything follows the active theme variant.
/// </summary>
internal static class UiTheme
{
    public const string AppBg = "Brb.AppBg";
    public const string Surface = "Brb.Surface";
    public const string SurfaceAlt = "Brb.SurfaceAlt";
    public const string Border_ = "Brb.Border";
    public const string TextPrimary = "Brb.TextPrimary";
    public const string TextSecondary = "Brb.TextSecondary";
    public const string Accent = "Brb.Accent";
    public const string AccentText = "Brb.AccentText";
    public const string Success = "Brb.Success";
    public const string Warn = "Brb.Warn";
    public const string Danger = "Brb.Danger";

    public static ResourceDictionary BuildPalette()
    {
        var dark = new ResourceDictionary
        {
            [AppBg] = B("#1C1D22"),
            [Surface] = B("#26272E"),
            [SurfaceAlt] = B("#32333B"),
            [Border_] = B("#3C3E47"),
            [TextPrimary] = B("#ECECF0"),
            [TextSecondary] = B("#A6A7B2"),
            [Accent] = B("#8071F2"),
            [AccentText] = B("#FFFFFF"),
            [Success] = B("#34D399"),
            [Warn] = B("#FBBF24"),
            [Danger] = B("#F87171"),
        };

        var light = new ResourceDictionary
        {
            [AppBg] = B("#F3F3F6"),
            [Surface] = B("#FFFFFF"),
            [SurfaceAlt] = B("#EDEDF1"),
            [Border_] = B("#E0E0E6"),
            [TextPrimary] = B("#1B1B1F"),
            [TextSecondary] = B("#6A6A73"),
            [Accent] = B("#6D5EF6"),
            [AccentText] = B("#FFFFFF"),
            [Success] = B("#16A34A"),
            [Warn] = B("#D97706"),
            [Danger] = B("#DC2626"),
        };

        var res = new ResourceDictionary();
        res.ThemeDictionaries[ThemeVariant.Dark] = dark;
        res.ThemeDictionaries[ThemeVariant.Light] = light;
        return res;
    }

    /// <summary>A few global control tweaks (rounded inputs).</summary>
    public static Styles BuildStyles()
    {
        var styles = new Styles();
        foreach (Type t in new[] { typeof(TextBox), typeof(NumericUpDown), typeof(ComboBox) })
        {
            var s = new Style(x => x.OfType(t));
            s.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(8)));
            styles.Add(s);
        }

        return styles;
    }

    private static SolidColorBrush B(string hex) => new(Color.Parse(hex));

    public static DynamicResourceExtension Dyn(string key) => new(key);

    // ---- Text ----

    public static TextBlock H1(string text) => Text(text, 19, FontWeight.SemiBold, TextPrimary);

    public static TextBlock H2(string text) => Text(text, 14.5, FontWeight.SemiBold, TextPrimary);

    public static TextBlock Body(string text) => Text(text, 13, FontWeight.Normal, TextPrimary);

    public static TextBlock Muted(string text) => Text(text, 12.5, FontWeight.Normal, TextSecondary);

    public static TextBlock SectionLabel(string text)
    {
        TextBlock t = Text(text.ToUpperInvariant(), 11, FontWeight.SemiBold, TextSecondary);
        t.LetterSpacing = 0.8;
        return t;
    }

    private static TextBlock Text(string text, double size, FontWeight weight, string fgKey)
    {
        var t = new TextBlock
        {
            Text = text,
            FontSize = size,
            FontWeight = weight,
            TextWrapping = TextWrapping.Wrap,
            [!TextBlock.ForegroundProperty] = Dyn(fgKey),
        };
        return t;
    }

    // ---- Surfaces ----

    public static Border Card(Control content, double padding = 16) => new()
    {
        Child = content,
        CornerRadius = new CornerRadius(14),
        BorderThickness = new Thickness(1),
        Padding = new Thickness(padding),
        [!Border.BackgroundProperty] = Dyn(Surface),
        [!Border.BorderBrushProperty] = Dyn(Border_),
        BoxShadow = new BoxShadows(new BoxShadow { OffsetX = 0, OffsetY = 3, Blur = 20, Color = Color.FromArgb(90, 0, 0, 0) }),
    };

    public static Border Chip(string text, string bgKey, string fgKey) => new()
    {
        CornerRadius = new CornerRadius(20),
        Padding = new Thickness(11, 4, 11, 5),
        VerticalAlignment = VerticalAlignment.Center,
        [!Border.BackgroundProperty] = Dyn(bgKey),
        Child = new TextBlock
        {
            Text = text,
            FontSize = 11.5,
            FontWeight = FontWeight.SemiBold,
            [!TextBlock.ForegroundProperty] = Dyn(fgKey),
        },
    };

    public static Border Dot(string colorKey) => new()
    {
        Width = 9,
        Height = 9,
        CornerRadius = new CornerRadius(5),
        VerticalAlignment = VerticalAlignment.Center,
        [!Border.BackgroundProperty] = Dyn(colorKey),
    };

    // ---- Buttons (Border-based for full control of the look) ----

    public static PillButton Primary(string text) => new(text, PillKind.Accent);

    public static PillButton Ghost(string text) => new(text, PillKind.Ghost);

    public static PillButton Icon(string glyph, string tip)
    {
        var b = new PillButton(glyph, PillKind.Icon) { Width = 34, Height = 34 };
        ToolTip.SetTip(b, tip);
        return b;
    }
}

internal enum PillKind
{
    Accent,
    Ghost,
    Icon,
}

/// <summary>A button drawn as a rounded pill, with full control over its colours.</summary>
internal sealed class PillButton : Border
{
    private readonly TextBlock _label;

    public PillButton(string text, PillKind kind)
    {
        string fg = kind == PillKind.Accent ? UiTheme.AccentText
            : kind == PillKind.Icon ? UiTheme.TextSecondary
            : UiTheme.TextPrimary;

        _label = new TextBlock
        {
            Text = text,
            FontSize = kind == PillKind.Icon ? 14 : 13,
            FontWeight = FontWeight.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            [!TextBlock.ForegroundProperty] = UiTheme.Dyn(fg),
        };

        Child = _label;
        CornerRadius = new CornerRadius(kind == PillKind.Icon ? 8 : 9);
        Padding = kind == PillKind.Icon ? new Thickness(0) : new Thickness(15, 9);
        Cursor = new Cursor(StandardCursorType.Hand);
        VerticalAlignment = VerticalAlignment.Center;

        switch (kind)
        {
            case PillKind.Accent:
                this[!BackgroundProperty] = UiTheme.Dyn(UiTheme.Accent);
                break;
            case PillKind.Ghost:
                this[!BackgroundProperty] = UiTheme.Dyn(UiTheme.SurfaceAlt);
                BorderThickness = new Thickness(1);
                this[!BorderBrushProperty] = UiTheme.Dyn(UiTheme.Border_);
                break;
            case PillKind.Icon:
                Background = Brushes.Transparent;
                break;
        }

        PointerEntered += (_, _) => Opacity = 0.8;
        PointerExited += (_, _) => Opacity = 1;
        AddHandler(Gestures.TappedEvent, (_, _) => Click?.Invoke(this, EventArgs.Empty));
    }

    public event EventHandler? Click;

    public string Text
    {
        get => _label.Text ?? "";
        set => _label.Text = value;
    }
}
