namespace BrainRotBlocker.Core.Configuration;

/// <summary>One curated, ready-to-pick blockable site.</summary>
public sealed class CatalogEntry
{
    public CatalogEntry(string id, string label, string url, bool includeSubpaths)
    {
        Id = id;
        Label = label;
        Url = url;
        IncludeSubpaths = includeSubpaths;
    }

    /// <summary>Stable identifier stored in rules so the entry stays canonical.</summary>
    public string Id { get; }

    /// <summary>Display name (a brand surface; not localized).</summary>
    public string Label { get; }

    public string Url { get; }

    public bool IncludeSubpaths { get; }

    public UrlPattern ToPattern() => SiteUrl.ToPattern(Url, IncludeSubpaths);
}

/// <summary>
/// The built-in catalog of common doom-scrolling surfaces the user can pick by
/// name, instead of typing hosts and paths. Custom sites are still allowed; the
/// catalog just removes the busywork for the common case.
/// </summary>
public static class SiteCatalog
{
    public static IReadOnlyList<CatalogEntry> Entries { get; } = new[]
    {
        new CatalogEntry("ig-reels", "Instagram Reels", "instagram.com/reels", true),
        new CatalogEntry("ig-feed", "Instagram feed", "instagram.com/", false),
        new CatalogEntry("yt-shorts", "YouTube Shorts", "youtube.com/shorts", true),
        new CatalogEntry("yt-home", "YouTube home", "youtube.com/", false),
        new CatalogEntry("tiktok", "TikTok", "tiktok.com", true),
        new CatalogEntry("fb-reels", "Facebook Reels", "facebook.com/reel", true),
        new CatalogEntry("fb-feed", "Facebook feed", "facebook.com/", false),
        new CatalogEntry("x", "X (Twitter)", "x.com", true),
        new CatalogEntry("reddit", "Reddit", "reddit.com", true),
        new CatalogEntry("linkedin", "LinkedIn feed", "linkedin.com/feed", true),
        new CatalogEntry("twitch", "Twitch", "twitch.tv", true),
        new CatalogEntry("pinterest", "Pinterest", "pinterest.com", true),
    };

    public static bool TryGet(string id, out CatalogEntry entry)
    {
        foreach (CatalogEntry candidate in Entries)
        {
            if (string.Equals(candidate.Id, id, StringComparison.Ordinal))
            {
                entry = candidate;
                return true;
            }
        }

        entry = null!;
        return false;
    }
}
