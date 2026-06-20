namespace BrainRotBlocker.Core.Configuration;

/// <summary>
/// A small built-in rule set used as a starting point and in tests. It is not
/// the product boundary: the real rule set is loaded from configuration so new
/// doom-scrolling surfaces can be added without recompiling (ADR-004).
/// </summary>
public static class DefaultConfiguration
{
    public const string ShortFormVideoBudgetId = "short-form-video";
    public const string FeedsBudgetId = "feeds";

    /// <summary>Builds the default configuration.</summary>
    public static BlockerConfiguration Create()
    {
        var shortForm = new BudgetGroup(
            ShortFormVideoBudgetId,
            "Short-form video",
            allowance: TimeSpan.FromMinutes(2),
            resetInterval: TimeSpan.FromHours(1));

        var feeds = new BudgetGroup(
            FeedsBudgetId,
            "Algorithmic feeds",
            allowance: TimeSpan.FromMinutes(2),
            resetInterval: TimeSpan.FromHours(1));

        var rules = new List<Rule>
        {
            new(
                "youtube-shorts",
                "YouTube Shorts",
                new UrlPattern("youtube.com", pathPrefixes: new[] { "/shorts" }),
                new[] { ShortFormVideoBudgetId }),
            new(
                "instagram-reels",
                "Instagram Reels",
                new UrlPattern("instagram.com", pathPrefixes: new[] { "/reels", "/reel" }),
                new[] { ShortFormVideoBudgetId }),
            new(
                "facebook-reels",
                "Facebook Reels",
                new UrlPattern("facebook.com", pathPrefixes: new[] { "/reel" }),
                new[] { ShortFormVideoBudgetId }),
            new(
                "tiktok-feed",
                "TikTok feed",
                new UrlPattern("tiktok.com", pathPrefixes: new[] { "/foryou", "/" }),
                new[] { ShortFormVideoBudgetId }),
            new(
                "instagram-home-feed",
                "Instagram home feed",
                new UrlPattern("instagram.com", pathRegex: "^/$"),
                new[] { FeedsBudgetId }),
        };

        return new BlockerConfiguration(rules, new[] { shortForm, feeds });
    }
}
