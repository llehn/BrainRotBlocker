using BrainRotBlocker.Core.Configuration;
using System.IO;
using System.Text.Json;

namespace BrainRotBlocker.App.Runtime;

internal sealed class StrictModeStore
{
    private readonly string _path;

    public StrictModeStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BrainRotBlocker",
            "strict-mode.json"))
    {
    }

    internal StrictModeStore(string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _path = path;
    }

    public StrictModeSnapshot GetSnapshot() => BuildSnapshot(ReadState(), DateTimeOffset.UtcNow);

    public bool TryLoadActiveConfiguration(out LoadedConfiguration? loaded)
    {
        loaded = null;
        StrictModeState? state = ReadState();
        StrictModeSnapshot snapshot = BuildSnapshot(state, DateTimeOffset.UtcNow);
        if (!snapshot.IsActive || string.IsNullOrWhiteSpace(state?.LockedConfigJson))
        {
            return false;
        }

        loaded = new LoadedConfiguration(
            ConfigurationLoader.Load(state.LockedConfigJson),
            "strict-mode locked config",
            state.LockedConfigJson,
            filePath: null);
        return true;
    }

    public StrictModeSnapshot Activate(TimeSpan duration, string configJson)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        StrictModeSnapshot current = BuildSnapshot(ReadState(), now);
        if (current.IsActive)
        {
            return current;
        }

        var state = new StrictModeState
        {
            ActivatedAtUtc = now,
            StrictUntilUtc = now + duration,
            LockedConfigJson = configJson,
        };
        File.WriteAllText(_path, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
        return BuildSnapshot(state, now);
    }

    private StrictModeState? ReadState()
    {
        if (!File.Exists(_path))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<StrictModeState>(File.ReadAllText(_path));
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static StrictModeSnapshot BuildSnapshot(StrictModeState? state, DateTimeOffset now)
    {
        if (state?.StrictUntilUtc is not { } until)
        {
            return StrictModeSnapshot.Inactive;
        }

        TimeSpan remaining = until - now;
        return remaining > TimeSpan.Zero
            ? new StrictModeSnapshot(true, until.ToLocalTime(), remaining)
            : StrictModeSnapshot.Inactive;
    }

    private sealed class StrictModeState
    {
        public DateTimeOffset ActivatedAtUtc { get; set; }
        public DateTimeOffset StrictUntilUtc { get; set; }
        public string LockedConfigJson { get; set; } = "";
    }
}

internal sealed class StrictModeSnapshot
{
    public static readonly StrictModeSnapshot Inactive = new(false, null, TimeSpan.Zero);

    public StrictModeSnapshot(bool isActive, DateTimeOffset? activeUntilLocal, TimeSpan remaining)
    {
        IsActive = isActive;
        ActiveUntilLocal = activeUntilLocal;
        Remaining = remaining;
    }

    public bool IsActive { get; }
    public DateTimeOffset? ActiveUntilLocal { get; }
    public TimeSpan Remaining { get; }
}
