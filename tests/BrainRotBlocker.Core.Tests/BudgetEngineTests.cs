using BrainRotBlocker.Core.Accounting;
using BrainRotBlocker.Core.Configuration;
using Xunit;

namespace BrainRotBlocker.Core.Tests;

public class BudgetEngineTests
{
    private static readonly DateTimeOffset T0 =
        new(2026, 6, 20, 12, 0, 0, TimeSpan.Zero);

    // Budgets used by the tests: small allowances so they exhaust quickly, and a
    // generous accounted-gap cap so a single multi-second tick counts in full.
    private const string ShortForm = "short-form";
    private const string Feeds = "feeds";

    private static BudgetEngine Engine(
        TimeSpan? allowance = null,
        TimeSpan? reset = null,
        TimeSpan? cap = null)
    {
        TimeSpan a = allowance ?? TimeSpan.FromSeconds(10);
        TimeSpan r = reset ?? TimeSpan.FromMinutes(1);

        var config = new BlockerConfiguration(
            new[]
            {
                new Rule(
                    "yt-shorts",
                    "YouTube Shorts",
                    new UrlPattern("youtube.com", pathPrefixes: new[] { "/shorts" }),
                    new[] { ShortForm }),
                new Rule(
                    "ig-reels",
                    "Instagram Reels",
                    new UrlPattern("instagram.com", pathPrefixes: new[] { "/reels" }),
                    new[] { ShortForm }),
                new Rule(
                    "ig-feed",
                    "Instagram home feed",
                    new UrlPattern("instagram.com", pathRegex: "^/$"),
                    new[] { Feeds }),
            },
            new[]
            {
                new BudgetGroup(ShortForm, ShortForm, a, r),
                new BudgetGroup(Feeds, Feeds, a, r),
            });

        return new BudgetEngine(
            config,
            new BudgetEngineOptions { MaxAccountedGap = cap ?? TimeSpan.FromMinutes(5) });
    }

    private static BrowserWindowState Win(string id, string? url) =>
        BrowserWindowState.FromString(id, url);

    private static IReadOnlyList<BrowserWindowState> Windows(params BrowserWindowState[] w) => w;

    private static TimeSpan Consumed(TickResult result, string budgetId) =>
        result.Budgets.Single(b => b.BudgetGroupId == budgetId).Consumed;

    private const string Shorts = "https://youtube.com/shorts/abc";
    private const string Reels = "https://instagram.com/reels/xyz";
    private const string IgFeed = "https://instagram.com/";
    private const string IgDirect = "https://instagram.com/direct/inbox";

    [Fact]
    public void First_tick_establishes_baseline_and_charges_nothing()
    {
        var engine = Engine();
        TickResult r = engine.Tick(Windows(Win("w1", Shorts)), T0);
        Assert.Equal(TimeSpan.Zero, Consumed(r, ShortForm));
        Assert.Empty(r.CloseDecisions);
    }

    [Fact]
    public void Matching_selected_tab_consumes_its_budget()
    {
        var engine = Engine();
        engine.Tick(Windows(Win("w1", Shorts)), T0);
        TickResult r = engine.Tick(Windows(Win("w1", Shorts)), T0 + TimeSpan.FromSeconds(4));
        Assert.Equal(TimeSpan.FromSeconds(4), Consumed(r, ShortForm));
    }

    [Fact]
    public void Non_matching_selected_tab_consumes_nothing()
    {
        var engine = Engine();
        engine.Tick(Windows(Win("w1", IgDirect)), T0);
        TickResult r = engine.Tick(Windows(Win("w1", IgDirect)), T0 + TimeSpan.FromSeconds(4));
        Assert.Equal(TimeSpan.Zero, Consumed(r, ShortForm));
        Assert.Empty(r.CloseDecisions);
    }

    [Fact]
    public void Budget_is_charged_once_across_multiple_matching_windows()
    {
        // ADR-005: time is not multiplied by the number of matching windows.
        var engine = Engine();
        engine.Tick(Windows(Win("w1", Shorts), Win("w2", Reels)), T0);
        TickResult r = engine.Tick(
            Windows(Win("w1", Shorts), Win("w2", Reels)),
            T0 + TimeSpan.FromSeconds(6));
        Assert.Equal(TimeSpan.FromSeconds(6), Consumed(r, ShortForm));
    }

    [Fact]
    public void Simultaneous_budget_groups_each_consume_the_same_elapsed()
    {
        var engine = Engine();
        engine.Tick(Windows(Win("w1", Shorts), Win("w2", IgFeed)), T0);
        TickResult r = engine.Tick(
            Windows(Win("w1", Shorts), Win("w2", IgFeed)),
            T0 + TimeSpan.FromSeconds(5));
        Assert.Equal(TimeSpan.FromSeconds(5), Consumed(r, ShortForm));
        Assert.Equal(TimeSpan.FromSeconds(5), Consumed(r, Feeds));
    }

    [Fact]
    public void Exhaustion_closes_the_matching_selected_tab_on_the_crossing_tick()
    {
        var engine = Engine(allowance: TimeSpan.FromSeconds(10));
        engine.Tick(Windows(Win("w1", Shorts)), T0);
        TickResult r = engine.Tick(Windows(Win("w1", Shorts)), T0 + TimeSpan.FromSeconds(10));

        CloseDecision decision = Assert.Single(r.CloseDecisions);
        Assert.Equal("w1", decision.WindowId);
        Assert.Equal(ShortForm, decision.BudgetGroupId);
        Assert.Equal("yt-shorts", decision.RuleId);
    }

    [Fact]
    public void Reopened_surface_is_closed_again_before_reset()
    {
        var engine = Engine(allowance: TimeSpan.FromSeconds(10), reset: TimeSpan.FromHours(1));
        engine.Tick(Windows(Win("w1", Shorts)), T0);
        engine.Tick(Windows(Win("w1", Shorts)), T0 + TimeSpan.FromSeconds(10)); // exhaust + close

        // User reopens shorts 30s later, still within the same budget window.
        TickResult r = engine.Tick(
            Windows(Win("w9", Shorts)),
            T0 + TimeSpan.FromSeconds(40));
        Assert.Equal("w9", Assert.Single(r.CloseDecisions).WindowId);
    }

    [Fact]
    public void Budget_becomes_available_again_after_window_reset()
    {
        var engine = Engine(allowance: TimeSpan.FromSeconds(10), reset: TimeSpan.FromMinutes(1));
        engine.Tick(Windows(Win("w1", Shorts)), T0);
        engine.Tick(Windows(Win("w1", Shorts)), T0 + TimeSpan.FromSeconds(10)); // exhaust

        // Next budget window (T0 is aligned to the minute, so +1 min is a new window).
        TickResult r = engine.Tick(
            Windows(Win("w1", Shorts)),
            T0 + TimeSpan.FromMinutes(1));
        Assert.Empty(r.CloseDecisions);
        Assert.Equal(TimeSpan.Zero, Consumed(r, ShortForm));
    }

    [Fact]
    public void Over_long_gap_is_clamped_so_sleep_does_not_drain_budget()
    {
        var engine = Engine(
            allowance: TimeSpan.FromSeconds(10),
            reset: TimeSpan.FromHours(24),
            cap: TimeSpan.FromSeconds(5));

        engine.Tick(Windows(Win("w1", Shorts)), T0);
        // Two-hour gap (machine asleep) but the same budget window.
        TickResult r = engine.Tick(Windows(Win("w1", Shorts)), T0 + TimeSpan.FromHours(2));
        Assert.Equal(TimeSpan.FromSeconds(5), Consumed(r, ShortForm));
        Assert.Empty(r.CloseDecisions);
    }

    [Fact]
    public void Clock_going_backwards_charges_nothing()
    {
        var engine = Engine();
        engine.Tick(Windows(Win("w1", Shorts)), T0);
        TickResult r = engine.Tick(Windows(Win("w1", Shorts)), T0 - TimeSpan.FromSeconds(30));
        Assert.Equal(TimeSpan.Zero, Consumed(r, ShortForm));
    }

    [Fact]
    public void Parked_tab_consumes_and_closes_only_once_it_becomes_selected()
    {
        // The model only ever sees the selected tab. While shorts is parked
        // (selected tab is DMs) nothing is charged; once it becomes selected it
        // consumes and can be closed.
        var engine = Engine(allowance: TimeSpan.FromSeconds(10));
        engine.Tick(Windows(Win("w1", IgDirect)), T0);
        TickResult parked = engine.Tick(Windows(Win("w1", IgDirect)), T0 + TimeSpan.FromSeconds(10));
        Assert.Equal(TimeSpan.Zero, Consumed(parked, ShortForm));

        engine.Tick(Windows(Win("w1", Shorts)), T0 + TimeSpan.FromSeconds(11));
        TickResult selected = engine.Tick(Windows(Win("w1", Shorts)), T0 + TimeSpan.FromSeconds(21));
        Assert.Equal("w1", Assert.Single(selected.CloseDecisions).WindowId);
    }

    [Fact]
    public void Window_with_no_url_is_ignored()
    {
        var engine = Engine();
        engine.Tick(Windows(Win("w1", null)), T0);
        TickResult r = engine.Tick(Windows(Win("w1", null)), T0 + TimeSpan.FromSeconds(5));
        Assert.Equal(TimeSpan.Zero, Consumed(r, ShortForm));
        Assert.Empty(r.CloseDecisions);
    }

    [Fact]
    public void Shared_budget_is_drained_by_either_surface()
    {
        // 6s of shorts then 6s of reels exhaust the shared 10s short-form budget.
        var engine = Engine(allowance: TimeSpan.FromSeconds(10), reset: TimeSpan.FromHours(1));
        engine.Tick(Windows(Win("w1", Shorts)), T0);
        engine.Tick(Windows(Win("w1", Shorts)), T0 + TimeSpan.FromSeconds(6));
        TickResult r = engine.Tick(Windows(Win("w1", Reels)), T0 + TimeSpan.FromSeconds(12));
        Assert.Equal("w1", Assert.Single(r.CloseDecisions).WindowId);
    }

    [Fact]
    public void Snapshot_reports_remaining_and_window_bounds()
    {
        var engine = Engine(allowance: TimeSpan.FromSeconds(10), reset: TimeSpan.FromMinutes(1));
        engine.Tick(Windows(Win("w1", Shorts)), T0);
        TickResult r = engine.Tick(Windows(Win("w1", Shorts)), T0 + TimeSpan.FromSeconds(4));

        BudgetSnapshot snap = r.Budgets.Single(b => b.BudgetGroupId == ShortForm);
        Assert.Equal(TimeSpan.FromSeconds(6), snap.Remaining);
        Assert.False(snap.IsExhausted);
        Assert.True(snap.WasActiveThisTick);
        Assert.Equal(T0, snap.WindowStart);
        Assert.Equal(T0 + TimeSpan.FromMinutes(1), snap.WindowEnd);
    }
}
