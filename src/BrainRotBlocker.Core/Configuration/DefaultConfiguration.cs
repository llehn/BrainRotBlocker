namespace BrainRotBlocker.Core.Configuration;

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
            new[]
            {
                Site("YouTube Shorts", "youtube.com/shorts"),
                Site("Instagram Reels", "instagram.com/reels"),
                Site("Facebook Reels", "facebook.com/reel"),
                Site("TikTok", "tiktok.com/foryou"),
            },
            allowance: TimeSpan.FromMinutes(5),
            allDay: true,
            from: new TimeOnly(0, 0),
            to: new TimeOnly(0, 0),
            days: null);

        var feeds = new Rule(
            FeedsRuleId,
            "Feeds",
            new[]
            {
                // The Instagram home feed only — keeps DMs and Stories reachable.
                Site("Instagram feed", "instagram.com/", includeSubpaths: false),
            },
            allowance: TimeSpan.FromMinutes(5),
            allDay: true,
            from: new TimeOnly(0, 0),
            to: new TimeOnly(0, 0),
            days: null);

        return new BlockerConfiguration(new[] { shortVideo, feeds });
    }

    private static TargetSite Site(string label, string url, bool includeSubpaths = true)
        => new(label, SiteUrl.ToPattern(url, includeSubpaths));
}
