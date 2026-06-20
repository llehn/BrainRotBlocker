namespace BrainRotBlocker.Core.Accounting;

/// <summary>
/// The decision to close one browser window's selected tab because the surface
/// it shows belongs to an exhausted budget.
/// </summary>
public sealed class CloseDecision
{
    public CloseDecision(string windowId, Uri url, string ruleId, string budgetGroupId)
    {
        WindowId = windowId;
        Url = url;
        RuleId = ruleId;
        BudgetGroupId = budgetGroupId;
    }

    public string WindowId { get; }

    public Uri Url { get; }

    /// <summary>The matching rule that identified the surface.</summary>
    public string RuleId { get; }

    /// <summary>The exhausted budget group that triggered the close.</summary>
    public string BudgetGroupId { get; }
}

/// <summary>Inspectable state of one budget group at a point in time.</summary>
public sealed class BudgetSnapshot
{
    public BudgetSnapshot(
        string budgetGroupId,
        TimeSpan consumed,
        TimeSpan allowance,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        bool wasActiveThisTick)
    {
        BudgetGroupId = budgetGroupId;
        Consumed = consumed;
        Allowance = allowance;
        WindowStart = windowStart;
        WindowEnd = windowEnd;
        WasActiveThisTick = wasActiveThisTick;
    }

    public string BudgetGroupId { get; }

    public TimeSpan Consumed { get; }

    public TimeSpan Allowance { get; }

    public TimeSpan Remaining =>
        Consumed >= Allowance ? TimeSpan.Zero : Allowance - Consumed;

    public bool IsExhausted => Consumed >= Allowance;

    /// <summary>Start of the current tumbling budget window.</summary>
    public DateTimeOffset WindowStart { get; }

    /// <summary>End of the current window; the budget resets at this instant.</summary>
    public DateTimeOffset WindowEnd { get; }

    /// <summary>True if a matching surface consumed this budget on the last tick.</summary>
    public bool WasActiveThisTick { get; }
}

/// <summary>The outcome of advancing the engine by one tick.</summary>
public sealed class TickResult
{
    public TickResult(
        IReadOnlyList<CloseDecision> closeDecisions,
        IReadOnlyList<BudgetSnapshot> budgets)
    {
        CloseDecisions = closeDecisions;
        Budgets = budgets;
    }

    /// <summary>
    /// The selected tabs to close this tick. At most one decision per window; a
    /// window selected for closing because of several exhausted budgets is
    /// reported once.
    /// </summary>
    public IReadOnlyList<CloseDecision> CloseDecisions { get; }

    /// <summary>Distinct window ids whose selected tab should be closed.</summary>
    public IEnumerable<string> WindowIdsToClose =>
        CloseDecisions.Select(d => d.WindowId);

    /// <summary>State of every budget group after this tick, for display and tests.</summary>
    public IReadOnlyList<BudgetSnapshot> Budgets { get; }
}
