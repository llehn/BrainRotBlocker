namespace BrainRotBlocker.Core.Accounting;

/// <summary>Tuning knobs for <see cref="BudgetEngine"/>.</summary>
public sealed class BudgetEngineOptions
{
    /// <summary>
    /// The largest gap between two ticks that is counted as real
    /// doom-scrolling time. A larger gap (system sleep, screen lock, the
    /// machine being too busy to tick) is clamped to this value so that, for
    /// example, a laptop asleep for two hours does not drain the budget. This
    /// realizes "system sleep and screen lock may pause accounting" (ADR-005)
    /// without any explicit power or idle detection.
    ///
    /// The default suits a monitor that polls roughly once per second.
    /// </summary>
    public TimeSpan MaxAccountedGap { get; init; } = TimeSpan.FromSeconds(5);
}
