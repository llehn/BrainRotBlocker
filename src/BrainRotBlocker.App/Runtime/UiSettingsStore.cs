using System.IO;
using System.Text.Json;

namespace BrainRotBlocker.App.Runtime;

internal enum ThemePreference
{
    System,
    Light,
    Dark,
}

/// <summary>Persists small UI preferences (currently just the theme choice).</summary>
internal sealed class UiSettingsStore
{
    private readonly string _path;

    public UiSettingsStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BrainRotBlocker",
            "ui-settings.json"))
    {
    }

    internal UiSettingsStore(string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _path = path;
    }

    public ThemePreference LoadTheme()
    {
        try
        {
            if (File.Exists(_path)
                && JsonSerializer.Deserialize<State>(File.ReadAllText(_path)) is { } state
                && Enum.TryParse(state.Theme, out ThemePreference theme))
            {
                return theme;
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
        }

        return ThemePreference.System;
    }

    public void SaveTheme(ThemePreference theme)
    {
        try
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(new State { Theme = theme.ToString() }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private sealed class State
    {
        public string Theme { get; set; } = nameof(ThemePreference.System);
    }
}
