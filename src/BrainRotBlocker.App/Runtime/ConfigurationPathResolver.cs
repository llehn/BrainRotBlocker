using System.IO;

namespace BrainRotBlocker.App.Runtime;

internal static class ConfigurationPathResolver
{
    public static string? FindDefaultConfig()
    {
        string[] starts =
        {
            Environment.CurrentDirectory,
            AppContext.BaseDirectory,
        };

        foreach (string start in starts)
        {
            DirectoryInfo? directory = new(start);
            while (directory is not null)
            {
                string candidate = Path.Combine(directory.FullName, "config", "default-config.json");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }
        }

        return null;
    }
}
