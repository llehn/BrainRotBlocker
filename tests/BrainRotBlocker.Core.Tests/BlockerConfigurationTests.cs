using BrainRotBlocker.Core.Configuration;
using Xunit;

namespace BrainRotBlocker.Core.Tests;

public class BlockerConfigurationTests
{
    private static BudgetGroup Budget(string id) =>
        new(id, id, TimeSpan.FromMinutes(2), TimeSpan.FromHours(1));

    private static Rule Rule(string id, UrlPattern p, params string[] budgets) =>
        new(id, id, p, budgets);

    [Fact]
    public void Rule_referencing_unknown_budget_is_rejected()
    {
        Assert.Throws<ConfigurationException>(() => new BlockerConfiguration(
            new[] { Rule("r", new UrlPattern("a.com"), "missing") },
            new[] { Budget("present") }));
    }

    [Fact]
    public void Duplicate_budget_ids_are_rejected()
    {
        Assert.Throws<ConfigurationException>(() => new BlockerConfiguration(
            Array.Empty<Rule>(),
            new[] { Budget("dup"), Budget("dup") }));
    }

    [Fact]
    public void Duplicate_rule_ids_are_rejected()
    {
        Assert.Throws<ConfigurationException>(() => new BlockerConfiguration(
            new[]
            {
                Rule("dup", new UrlPattern("a.com"), "b"),
                Rule("dup", new UrlPattern("c.com"), "b"),
            },
            new[] { Budget("b") }));
    }

    [Fact]
    public void BudgetGroupIdsForUrl_unions_budgets_of_all_matching_rules()
    {
        var config = new BlockerConfiguration(
            new[]
            {
                Rule("reels", new UrlPattern("instagram.com", pathPrefixes: new[] { "/reels" }), "short-form"),
                // A second rule that also matches /reels and adds another budget.
                Rule("ig-any", new UrlPattern("instagram.com"), "all-instagram"),
            },
            new[] { Budget("short-form"), Budget("all-instagram") });

        ISet<string> budgets = config.BudgetGroupIdsForUrl(new Uri("https://instagram.com/reels/x"));
        Assert.Equal(new[] { "all-instagram", "short-form" }, budgets.OrderBy(x => x));
    }

    [Fact]
    public void Default_configuration_is_valid_and_matches_known_surfaces()
    {
        BlockerConfiguration config = DefaultConfiguration.Create();

        Assert.Contains(
            DefaultConfiguration.ShortFormVideoBudgetId,
            config.BudgetGroupIdsForUrl(new Uri("https://www.youtube.com/shorts/x")));
        Assert.Contains(
            DefaultConfiguration.ShortFormVideoBudgetId,
            config.BudgetGroupIdsForUrl(new Uri("https://instagram.com/reels/x")));
        Assert.Contains(
            DefaultConfiguration.FeedsBudgetId,
            config.BudgetGroupIdsForUrl(new Uri("https://instagram.com/")));

        // Instagram DMs must remain unaffected.
        Assert.Empty(config.BudgetGroupIdsForUrl(new Uri("https://instagram.com/direct/inbox")));
        // A normal YouTube watch page must remain unaffected.
        Assert.Empty(config.BudgetGroupIdsForUrl(new Uri("https://www.youtube.com/watch?v=x")));
    }
}
