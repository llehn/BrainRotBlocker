using System.IO;

namespace BrainRotDoctor.App.Runtime.Update;

/// <summary>
/// Startup housekeeping for the updater. A freshly launched primary is, by
/// definition, the end of any update: it consumes the stand-down marker (so no
/// running instance keeps standing down) and clears the staged binary and the
/// <c>.bak</c> backup left by a swap. All best-effort — a leftover that is still
/// locked is simply retried on the next launch.
/// </summary>
internal static class UpdateRecovery
{
    public static void CleanupOnStartup(string installedExePath)
    {
        var paths = UpdatePaths.ForCurrentProcess(installedExePath);
        new UpdateStanddown(paths).DeleteMarker();

        TryDeleteDirectory(paths.StagedDir);
        TryDeleteFile(installedExePath + ".bak");
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
