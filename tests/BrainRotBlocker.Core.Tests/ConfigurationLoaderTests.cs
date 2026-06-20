using BrainRotBlocker.Core.Configuration;
using Xunit;

namespace BrainRotBlocker.Core.Tests;

public class ConfigurationLoaderTests
{
    [Fact]
    public void Loads_a_valid_configuration_from_json()
    {
        const string json = """
        {
          "budgetGroups": [
            { "id": "short-form-video", "name": "Short-form video",
              "allowance": "2m", "resetInterval": "1h" }
          ],
          "rules": [
            { "id": "youtube-shorts", "name": "YouTube Shorts",
              "host": "youtube.com", "pathPrefixes": ["/shorts"],
              "budgets": ["short-form-video"] }
          ]
        }
        """;

        BlockerConfiguration config = ConfigurationLoader.Load(json);

        BudgetGroup budget = Assert.Single(config.BudgetGroups);
        Assert.Equal(TimeSpan.FromMinutes(2), budget.Allowance);
        Assert.Equal(TimeSpan.FromHours(1), budget.ResetInterval);

        Assert.Contains(
            "short-form-video",
            config.BudgetGroupIdsForUrl(new Uri("https://youtube.com/shorts/x")));
    }

    [Fact]
    public void Rule_referencing_unknown_budget_is_rejected()
    {
        const string json = """
        {
          "budgetGroups": [],
          "rules": [
            { "id": "r", "host": "a.com", "budgets": ["missing"] }
          ]
        }
        """;

        Assert.Throws<ConfigurationException>(() => ConfigurationLoader.Load(json));
    }

    [Fact]
    public void Malformed_json_throws_configuration_exception()
    {
        Assert.Throws<ConfigurationException>(() => ConfigurationLoader.Load("{ not json"));
    }

    [Theory]
    [InlineData("2m", 0, 2, 0)]
    [InlineData("1h", 1, 0, 0)]
    [InlineData("90s", 0, 1, 30)]
    [InlineData("1h30m", 1, 30, 0)]
    [InlineData("00:02:00", 0, 2, 0)]
    public void Duration_parses_friendly_forms(string text, int h, int m, int s)
    {
        Assert.Equal(new TimeSpan(h, m, s), Duration.Parse(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("2x")]
    [InlineData("abc")]
    [InlineData("10")]
    public void Duration_rejects_invalid_forms(string text)
    {
        Assert.False(Duration.TryParse(text, out _));
    }
}
