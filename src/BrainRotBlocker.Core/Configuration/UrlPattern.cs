using System.Text.RegularExpressions;

namespace BrainRotBlocker.Core.Configuration;

/// <summary>
/// Identifies a doom-scrolling surface by matching against a URL.
///
/// A pattern matches a URL when the host constraint matches AND the path
/// constraint matches. When several path constraints are configured
/// (<see cref="PathPrefixes"/> and <see cref="PathRegex"/>) they are combined
/// with OR: the path matches if any configured path constraint matches. When no
/// path constraint is configured, the path is unconstrained.
///
/// Only absolute http/https URLs are ever matched; anything else
/// (about:blank, file://, malformed) never matches.
/// </summary>
public sealed class UrlPattern
{
    private readonly Regex? _pathRegex;

    /// <param name="hostSuffix">
    /// Matches a host that equals this value or ends with "." + this value
    /// (case-insensitive). For example "youtube.com" matches "youtube.com" and
    /// "www.youtube.com" but not "notyoutube.com". Null means any host.
    /// </param>
    /// <param name="pathPrefixes">
    /// The path matches if it starts (ordinal, case-insensitive) with any of
    /// these. Null or empty means no prefix constraint.
    /// </param>
    /// <param name="pathRegex">
    /// The path matches if this regular expression matches it. Null means no
    /// regex constraint.
    /// </param>
    public UrlPattern(
        string? hostSuffix = null,
        IReadOnlyList<string>? pathPrefixes = null,
        string? pathRegex = null)
    {
        HostSuffix = Normalize(hostSuffix);
        PathPrefixes = pathPrefixes is { Count: > 0 }
            ? pathPrefixes.ToArray()
            : Array.Empty<string>();
        PathRegex = string.IsNullOrWhiteSpace(pathRegex) ? null : pathRegex;

        if (PathRegex is not null)
        {
            try
            {
                _pathRegex = new Regex(
                    PathRegex,
                    RegexOptions.CultureInvariant | RegexOptions.Compiled);
            }
            catch (ArgumentException ex)
            {
                throw new ConfigurationException(
                    $"Invalid path regular expression '{PathRegex}': {ex.Message}",
                    ex);
            }
        }

        if (HostSuffix is null && PathPrefixes.Count == 0 && PathRegex is null)
        {
            throw new ConfigurationException(
                "A URL pattern must constrain at least the host or the path; " +
                "a pattern that matches every URL is not allowed.");
        }
    }

    public string? HostSuffix { get; }

    public IReadOnlyList<string> PathPrefixes { get; }

    public string? PathRegex { get; }

    /// <summary>Returns true if the given URL matches this pattern.</summary>
    public bool IsMatch(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        if (!uri.IsAbsoluteUri)
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        if (!HostMatches(uri.Host))
        {
            return false;
        }

        return PathMatches(uri.AbsolutePath);
    }

    private bool HostMatches(string host)
    {
        if (HostSuffix is null)
        {
            return true;
        }

        host = host.TrimEnd('.');
        if (string.Equals(host, HostSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return host.EndsWith("." + HostSuffix, StringComparison.OrdinalIgnoreCase);
    }

    private bool PathMatches(string path)
    {
        bool hasPrefixes = PathPrefixes.Count > 0;
        bool hasRegex = _pathRegex is not null;

        if (!hasPrefixes && !hasRegex)
        {
            return true;
        }

        if (hasPrefixes)
        {
            foreach (string prefix in PathPrefixes)
            {
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return hasRegex && _pathRegex!.IsMatch(path);
    }

    private static string? Normalize(string? hostSuffix)
    {
        if (string.IsNullOrWhiteSpace(hostSuffix))
        {
            return null;
        }

        return hostSuffix.Trim().TrimStart('.').TrimEnd('.').ToLowerInvariant();
    }
}
