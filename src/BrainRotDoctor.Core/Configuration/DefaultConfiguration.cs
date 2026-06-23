namespace BrainRotDoctor.Core.Configuration;

/// <summary>
/// A small built-in configuration used as a starting point and in tests. It is
/// not the product boundary: the real configuration is loaded from a file so new
/// rules can be added without recompiling.
/// </summary>
public static class DefaultConfiguration
{
    public const string ShortVideoRuleId = "short-video";
    public const string FeedsRuleId = "feeds";

    /// <summary>Builds the default configuration.</summary>
    public static BlockerConfiguration Create()
    {
        var shortVideo = new Rule(
            ShortVideoRuleId,
            "Short video",
            new[] { Catalog("yt-shorts"), Catalog("ig-reels"), Catalog("fb-reels"), Catalog("tiktok") },
            allowance: TimeSpan.FromMinutes(5),
            allDay: true,
            from: new TimeOnly(0, 0),
            to: new TimeOnly(0, 0),
            days: null);

        var feeds = new Rule(
            FeedsRuleId,
            "Feeds",
            new[] { Catalog("ig-feed") },
            allowance: TimeSpan.FromMinutes(5),
            allDay: true,
            from: new TimeOnly(0, 0),
            to: new TimeOnly(0, 0),
            days: null);

        return new BlockerConfiguration(new[] { shortVideo, feeds });
    }

    private static TargetSite Catalog(string id)
    {
        CatalogEntry entry = SiteCatalog.Entries.First(e => e.Id == id);
        return new TargetSite(entry.Label, entry.ToPattern());
    }
}
