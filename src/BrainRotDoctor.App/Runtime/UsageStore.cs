using BrainRotDoctor.Core.Accounting;
using System.IO;
using System.Text.Json;

namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// Persists the current hour's allowance usage so it survives a config save, an
/// app restart, and an update swap. Without it, rebuilding the accounting engine —
/// which any of those does — would silently reset every rule's hour to zero, an
/// easy way to dodge a limit. The file lives in per-user app data next to the
/// config, so a real uninstall removes it while an upgrade leaves it untouched.
/// </summary>
internal sealed class UsageStore
{
    private readonly string _path;

    public UsageStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BrainRotDoctor",
            "usage.json"))
    {
    }

    internal UsageStore(string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _path = path;
    }

    public IReadOnlyList<RuleUsage> Load()
    {
        try
        {
            if (File.Exists(_path)
                && JsonSerializer.Deserialize<List<RuleUsage>>(File.ReadAllText(_path)) is { } usage)
            {
                return usage;
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
        }

        return Array.Empty<RuleUsage>();
    }

    public void Save(IReadOnlyList<RuleUsage> usage)
    {
        try
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(usage));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
