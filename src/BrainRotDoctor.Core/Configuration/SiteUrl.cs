using System.Text.RegularExpressions;

namespace BrainRotDoctor.Core.Configuration;

/// <summary>
/// Turns the URL a user types ("instagram.com/reels") into a
/// <see cref="UrlPattern"/>, so the user never has to think about hosts, path
/// prefixes, or regular expressions. The rules are intentionally simple:
/// <list type="bullet">
///   <item>A bare host ("tiktok.com") matches the whole site.</item>
///   <item>A host with a path ("instagram.com/reels") matches that path and,
///   when <c>includeSubpaths</c> is set, everything beneath it.</item>
/// </list>
/// </summary>
public static class SiteUrl
{
    /// <summary>
    /// Parses <paramref name="url"/> and builds the matching pattern.
    /// </summary>
    /// <param name="url">The website address the user typed.</param>
    /// <param name="includeSubpaths">
    /// When the URL has a path, whether to also match pages beneath it. Ignored
    /// for a bare host (which always matches the whole site).
    /// </param>
    public static UrlPattern ToPattern(string url, bool includeSubpaths)
    {
        (string host, string path) = Parse(url);
        path = path.TrimEnd('/');

        if (path.Length == 0)
        {
            // Root address: "include subpaths" means the whole site; otherwise
            // only the front page (so e.g. Instagram DMs stay reachable).
            return includeSubpaths
                ? new UrlPattern(host)
                : new UrlPattern(host, pathRegex: "^/?$");
        }

        return includeSubpaths
            ? new UrlPattern(host, pathPrefixes: new[] { path })
            : new UrlPattern(host, pathRegex: $"^{Regex.Escape(path)}/?$");
    }

    /// <summary>Extracts the normalized host and path from a typed URL.</summary>
    public static (string Host, string Path) Parse(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ConfigurationException("A site URL must not be empty.");
        }

        string text = url.Trim();
        if (!text.Contains("://", StringComparison.Ordinal))
        {
            text = "https://" + text;
        }

        if (!Uri.TryCreate(text, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ConfigurationException($"'{url}' is not a valid website address.");
        }

        string host = uri.Host;
        if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            host = host[4..];
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ConfigurationException($"'{url}' is missing a website host.");
        }

        return (host, uri.AbsolutePath);
    }
}
