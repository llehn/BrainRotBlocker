using BrainRotBlocker.Core.Configuration;
using Xunit;

namespace BrainRotBlocker.Core.Tests;

/// <summary>
/// Guards the shipped <c>config/default-config.json</c>: it must always parse,
/// validate, and recognize the same surfaces as the in-code default.
/// </summary>
public class ShippedConfigTests
{
    private static BlockerConfiguration LoadShipped()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "config", "default-config.json");
        return ConfigurationLoader.LoadFile(path);
    }

    [Fact]
    public void Shipped_config_loads_and_validates()
    {
        BlockerConfiguration config = LoadShipped();
        Assert.NotEmpty(config.Rules);
        Assert.NotEmpty(config.BudgetGroups);
    }

    [Theory]
    [InlineData("https://www.youtube.com/shorts/x", DefaultConfiguration.ShortFormVideoBudgetId)]
    [InlineData("https://instagram.com/reels/x", DefaultConfiguration.ShortFormVideoBudgetId)]
    [InlineData("https://instagram.com/", DefaultConfiguration.FeedsBudgetId)]
    public void Shipped_config_matches_known_surfaces(string url, string expectedBudget)
    {
        BlockerConfiguration config = LoadShipped();
        Assert.Contains(expectedBudget, config.BudgetGroupIdsForUrl(new Uri(url)));
    }

    [Theory]
    [InlineData("https://instagram.com/direct/inbox")]
    [InlineData("https://www.youtube.com/watch?v=x")]
    public void Shipped_config_leaves_useful_surfaces_alone(string url)
    {
        BlockerConfiguration config = LoadShipped();
        Assert.Empty(config.BudgetGroupIdsForUrl(new Uri(url)));
    }
}
