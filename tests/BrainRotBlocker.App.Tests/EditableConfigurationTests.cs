using BrainRotBlocker.App.Runtime;
using BrainRotBlocker.Core.Configuration;
using Xunit;

namespace BrainRotBlocker.App.Tests;

public sealed class EditableConfigurationTests
{
    [Fact]
    public void Round_trips_allowance_and_block_completely_rules()
    {
        const string json = """
        {
          "rules": [
            {
              "id": "short-video", "name": "Short video",
              "allowanceMinutes": 5, "allDay": true,
              "sites": [
                { "label": "YouTube Shorts", "url": "youtube.com/shorts", "includeSubpaths": true }
              ]
            },
            {
              "id": "bedtime", "name": "Bedtime",
              "allDay": false, "from": "23:00", "to": "07:00", "days": ["Monday"],
              "sites": [ { "label": "Instagram", "url": "instagram.com" } ]
            }
          ]
        }
        """;

        EditableConfiguration editable = EditableConfiguration.FromJson(json);
        Assert.Equal(2, editable.Rules.Count);

        EditableConfiguration.EditableRule shortVideo = editable.Rules[0];
        Assert.False(shortVideo.BlockCompletely);
        Assert.Equal(5, shortVideo.AllowanceMinutes);
        Assert.True(shortVideo.AllDay);
        Assert.Equal("YouTube Shorts", Assert.Single(shortVideo.Sites).Label);

        EditableConfiguration.EditableRule bedtime = editable.Rules[1];
        Assert.True(bedtime.BlockCompletely);
        Assert.False(bedtime.AllDay);
        Assert.Equal(new TimeOnly(23, 0), bedtime.From);
        Assert.Equal(new[] { DayOfWeek.Monday }, bedtime.Days);

        // Re-serialize, reload, and verify matching still works.
        BlockerConfiguration blocker = EditableConfiguration
            .FromJson(editable.ToJson())
            .ToBlockerConfiguration();

        Assert.Contains(blocker.MatchingRules(new Uri("https://youtube.com/shorts/x")),
            r => r.Id == "short-video");
        Assert.Contains(blocker.MatchingRules(new Uri("https://instagram.com/direct")),
            r => r.Id == "bedtime");
    }

    [Fact]
    public void Editing_a_site_url_changes_what_is_matched()
    {
        EditableConfiguration editable = EditableConfiguration.FromJson("""
        {
          "rules": [
            { "id": "r", "name": "R", "allowanceMinutes": 5, "allDay": true,
              "sites": [ { "label": "Reels", "url": "instagram.com/reels" } ] }
          ]
        }
        """);

        editable.Rules[0].Sites[0].Url = "tiktok.com/foryou";

        BlockerConfiguration blocker = editable.ToBlockerConfiguration();
        Assert.NotEmpty(blocker.MatchingRules(new Uri("https://tiktok.com/foryou/x")));
        Assert.Empty(blocker.MatchingRules(new Uri("https://instagram.com/reels/x")));
    }
}
