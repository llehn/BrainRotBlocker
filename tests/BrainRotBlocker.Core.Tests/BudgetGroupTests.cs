using BrainRotBlocker.Core.Configuration;
using Xunit;

namespace BrainRotBlocker.Core.Tests;

public class BudgetGroupTests
{
    private static readonly DateTimeOffset Epoch = DateTimeOffset.UnixEpoch;

    [Fact]
    public void Window_aligns_to_anchor_for_hourly_interval()
    {
        var group = new BudgetGroup("g", "g", TimeSpan.FromMinutes(2), TimeSpan.FromHours(1));

        var instant = new DateTimeOffset(2026, 6, 20, 14, 37, 12, TimeSpan.Zero);
        Assert.Equal(
            new DateTimeOffset(2026, 6, 20, 14, 0, 0, TimeSpan.Zero),
            group.WindowStartFor(instant));
    }

    [Fact]
    public void Consecutive_instants_in_same_hour_share_a_window()
    {
        var group = new BudgetGroup("g", "g", TimeSpan.FromMinutes(2), TimeSpan.FromHours(1));
        var a = new DateTimeOffset(2026, 6, 20, 14, 1, 0, TimeSpan.Zero);
        var b = new DateTimeOffset(2026, 6, 20, 14, 59, 59, TimeSpan.Zero);
        Assert.Equal(group.WindowStartFor(a), group.WindowStartFor(b));
    }

    [Fact]
    public void Crossing_the_interval_boundary_changes_the_window()
    {
        var group = new BudgetGroup("g", "g", TimeSpan.FromMinutes(2), TimeSpan.FromHours(1));
        var a = new DateTimeOffset(2026, 6, 20, 14, 59, 59, TimeSpan.Zero);
        var b = new DateTimeOffset(2026, 6, 20, 15, 0, 0, TimeSpan.Zero);
        Assert.NotEqual(group.WindowStartFor(a), group.WindowStartFor(b));
    }

    [Fact]
    public void Window_start_is_independent_of_input_timezone()
    {
        var group = new BudgetGroup("g", "g", TimeSpan.FromMinutes(2), TimeSpan.FromHours(1));
        var utc = new DateTimeOffset(2026, 6, 20, 14, 30, 0, TimeSpan.Zero);
        var plus2 = new DateTimeOffset(2026, 6, 20, 16, 30, 0, TimeSpan.FromHours(2));
        Assert.Equal(group.WindowStartFor(utc), group.WindowStartFor(plus2));
    }

    [Fact]
    public void Instant_before_anchor_floors_correctly()
    {
        var group = new BudgetGroup("g", "g", TimeSpan.FromMinutes(2), TimeSpan.FromHours(1));
        var before = Epoch - TimeSpan.FromMinutes(30);
        Assert.Equal(Epoch - TimeSpan.FromHours(1), group.WindowStartFor(before));
    }

    [Fact]
    public void Non_positive_allowance_is_rejected()
    {
        Assert.Throws<ConfigurationException>(
            () => new BudgetGroup("g", "g", TimeSpan.Zero, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void Allowance_exceeding_interval_is_rejected()
    {
        Assert.Throws<ConfigurationException>(
            () => new BudgetGroup("g", "g", TimeSpan.FromHours(2), TimeSpan.FromHours(1)));
    }
}
