using BrainRotBlocker.App.Runtime;
using System.IO;
using Xunit;

namespace BrainRotBlocker.App.Tests;

public sealed class StrictModeStoreTests
{
    [Fact]
    public void Active_strict_mode_loads_locked_configuration_snapshot()
    {
        string dir = Path.Combine(Path.GetTempPath(), "brainrotblocker-tests", Guid.NewGuid().ToString("N"));
        string path = Path.Combine(dir, "strict-mode.json");
        var store = new StrictModeStore(path);
        const string json = """
        {
          "rules": [
            { "id": "locked", "name": "Locked", "allowanceMinutes": 1, "allDay": true,
              "sites": [ { "label": "Locked", "url": "example.com/locked" } ] }
          ]
        }
        """;

        StrictModeSnapshot snapshot = store.Activate(TimeSpan.FromHours(1), json);

        Assert.True(snapshot.IsActive);
        Assert.True(store.TryLoadActiveConfiguration(out LoadedConfiguration? loaded));
        Assert.NotNull(loaded);
        Assert.Contains(
            loaded.Configuration.MatchingRules(new Uri("https://example.com/locked")),
            r => r.Id == "locked");
    }
}
