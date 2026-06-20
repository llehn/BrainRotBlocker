using BrainRotBlocker.Core.Configuration;

namespace BrainRotBlocker.Core.Accounting;

/// <summary>
/// The deterministic core of the product: given the selected tab of every open
/// browser window and the current time, it accounts time against the configured
/// budgets and decides which selected tabs must be closed.
///
/// The engine owns no clock and touches no OS resources. The caller supplies the
/// browser snapshot and the current instant on every <see cref="Tick"/>, which
/// makes the whole model reproducible and unit-testable.
///
/// Accounting rules (ADR-005):
/// <list type="bullet">
///   <item>Only the selected tab / current page of each window is considered.</item>
///   <item>Each affected budget is charged the elapsed interval exactly once per
///   tick, never multiplied by the number of matching windows.</item>
///   <item>An over-long gap between ticks is clamped (see
///   <see cref="BudgetEngineOptions.MaxAccountedGap"/>) so sleep/lock effectively
///   pause accounting.</item>
/// </list>
///
/// Closing rules (ADR-004): a window's selected tab is closed when it matches a
/// rule assigned to any exhausted budget. The order within a tick is: reset
/// windows that rolled over, charge elapsed time, then evaluate exhaustion, so
/// the very tick that exhausts a budget also closes the surface that drained it.
/// </summary>
public sealed class BudgetEngine
{
    private readonly BlockerConfiguration _configuration;
    private readonly BudgetEngineOptions _options;
    private readonly Dictionary<string, BudgetRuntimeState> _states;
    private DateTimeOffset? _lastTick;

    public BudgetEngine(
        BlockerConfiguration configuration,
        BudgetEngineOptions? options = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _options = options ?? new BudgetEngineOptions();
        _states = new Dictionary<string, BudgetRuntimeState>(StringComparer.Ordinal);
        foreach (BudgetGroup group in _configuration.BudgetGroups)
        {
            _states[group.Id] = new BudgetRuntimeState(group);
        }
    }

    /// <summary>
    /// Advances accounting to <paramref name="now"/> for the given browser
    /// windows and returns the close decisions plus the post-tick budget state.
    ///
    /// The first call after construction only establishes the time baseline (it
    /// charges no time) but still evaluates blocking against any state restored
    /// from persistence.
    /// </summary>
    public TickResult Tick(IReadOnlyList<BrowserWindowState> windows, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(windows);
        now = now.ToUniversalTime();

        TimeSpan elapsed = ComputeElapsed(now);
        _lastTick = now;

        // Roll every budget over to the window that contains `now` before we
        // charge time, so a brand-new window starts from a clean allowance.
        foreach (BudgetRuntimeState state in _states.Values)
        {
            state.RollTo(now);
        }

        // Determine which budgets are consumed this tick: the union over all
        // windows of the budgets each selected URL maps to. Using a set means a
        // budget is charged once even when several windows show a matching page.
        var affectedBudgets = new HashSet<string>(StringComparer.Ordinal);
        foreach (BrowserWindowState window in windows)
        {
            if (window.Url is null)
            {
                continue;
            }

            foreach (string budgetId in _configuration.BudgetGroupIdsForUrl(window.Url))
            {
                affectedBudgets.Add(budgetId);
            }
        }

        if (elapsed > TimeSpan.Zero)
        {
            foreach (string budgetId in affectedBudgets)
            {
                _states[budgetId].Charge(elapsed, now);
            }
        }

        // Decide closures: a window is closed when its selected page matches a
        // rule assigned to an exhausted budget. One decision per window.
        var decisions = new List<CloseDecision>();
        foreach (BrowserWindowState window in windows)
        {
            if (window.Url is null)
            {
                continue;
            }

            CloseDecision? decision = EvaluateClose(window);
            if (decision is not null)
            {
                decisions.Add(decision);
            }
        }

        return new TickResult(decisions, BuildSnapshots(affectedBudgets));
    }

    /// <summary>Current state of every budget without advancing time.</summary>
    public IReadOnlyList<BudgetSnapshot> GetBudgetSnapshots()
        => BuildSnapshots(activeBudgets: null);

    private CloseDecision? EvaluateClose(BrowserWindowState window)
    {
        foreach (Rule rule in _configuration.MatchingRules(window.Url!))
        {
            foreach (string budgetId in rule.BudgetGroupIds)
            {
                if (_states[budgetId].IsExhausted)
                {
                    return new CloseDecision(window.WindowId, window.Url!, rule.Id, budgetId);
                }
            }
        }

        return null;
    }

    private TimeSpan ComputeElapsed(DateTimeOffset now)
    {
        if (_lastTick is not { } last)
        {
            return TimeSpan.Zero;
        }

        TimeSpan raw = now - last;
        if (raw <= TimeSpan.Zero)
        {
            // Clock did not advance (or went backwards). Charge nothing.
            return TimeSpan.Zero;
        }

        return raw > _options.MaxAccountedGap ? _options.MaxAccountedGap : raw;
    }

    private IReadOnlyList<BudgetSnapshot> BuildSnapshots(ISet<string>? activeBudgets)
    {
        var snapshots = new List<BudgetSnapshot>(_states.Count);
        foreach (BudgetRuntimeState state in _states.Values)
        {
            snapshots.Add(state.ToSnapshot(
                activeBudgets?.Contains(state.Group.Id) ?? false));
        }

        return snapshots;
    }

    /// <summary>Mutable per-budget runtime state: current window and time used.</summary>
    private sealed class BudgetRuntimeState
    {
        private DateTimeOffset _windowStart;
        private TimeSpan _consumed;

        public BudgetRuntimeState(BudgetGroup group)
        {
            Group = group;
            _windowStart = group.WindowStartFor(group.Anchor);
            _consumed = TimeSpan.Zero;
        }

        public BudgetGroup Group { get; }

        public bool IsExhausted => _consumed >= Group.Allowance;

        public void RollTo(DateTimeOffset now)
        {
            DateTimeOffset windowStart = Group.WindowStartFor(now);
            if (windowStart != _windowStart)
            {
                _windowStart = windowStart;
                _consumed = TimeSpan.Zero;
            }
        }

        public void Charge(TimeSpan elapsed, DateTimeOffset now)
        {
            // Only the portion of the elapsed gap that falls inside the current
            // window may be charged to it. When a tick spans a window boundary,
            // the time before the boundary belonged to the (now reset) previous
            // window and must not over-charge the fresh allowance.
            TimeSpan withinWindow = now - _windowStart;
            if (elapsed > withinWindow)
            {
                elapsed = withinWindow;
            }

            if (elapsed <= TimeSpan.Zero)
            {
                return;
            }

            _consumed += elapsed;
            if (_consumed > Group.Allowance)
            {
                // Cap so the consumed value stays tidy; exhaustion is sticky
                // until the window resets regardless.
                _consumed = Group.Allowance;
            }
        }

        public BudgetSnapshot ToSnapshot(bool wasActiveThisTick) => new(
            Group.Id,
            _consumed,
            Group.Allowance,
            _windowStart,
            _windowStart + Group.ResetInterval,
            wasActiveThisTick);
    }
}
