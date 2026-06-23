using System.Reflection;

namespace BrainRotDoctor.App.Runtime;

/// <summary>
/// The running build's version. Sourced from the assembly's informational
/// version (set by <c>&lt;Version&gt;</c> in Directory.Build.props, overridden
/// from the release tag in CI). The auto-updater compares this against the
/// latest published manifest to decide whether to update (forward only).
/// </summary>
internal static class AppVersion
{
    /// <summary>The current build version, or 0.0.0 if it cannot be resolved.</summary>
    public static Version Current { get; } = Resolve();

    private static Version Resolve()
    {
        Assembly assembly = typeof(AppVersion).Assembly;

        // InformationalVersion carries the SemVer set by <Version>; with a
        // single-file/CI build it may have a "+<commit>" suffix to strip.
        string? informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (TryParse(informational, out Version? fromInformational))
        {
            return fromInformational;
        }

        return assembly.GetName().Version ?? new Version(0, 0, 0);
    }

    private static bool TryParse(string? raw, out Version version)
    {
        version = new Version(0, 0, 0);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        int plus = raw.IndexOf('+');
        string trimmed = plus >= 0 ? raw[..plus] : raw;
        return Version.TryParse(trimmed, out version!);
    }
}
