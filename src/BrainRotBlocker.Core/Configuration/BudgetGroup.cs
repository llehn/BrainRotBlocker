namespace BrainRotBlocker.Core.Configuration;

/// <summary>
/// A recurring time budget for one or more doom-scrolling surfaces, for example
/// "2 minutes every 1 hour".
///
/// The budget resets on fixed tumbling windows of length
/// <see cref="ResetInterval"/> aligned to <see cref="Anchor"/>. With the default
/// anchor (Unix epoch, UTC) a one-hour interval produces windows aligned to the
/// top of each UTC hour. Tumbling (rather than rolling) windows keep accounting
/// deterministic and cheap, in line with the product's preference for the
/// simplest model that works.
/// </summary>
public sealed class BudgetGroup
{
    /// <param name="id">Stable identifier referenced by rules.</param>
    /// <param name="name">Human-readable name for display.</param>
    /// <param name="allowance">Allowed time per window. Must be positive.</param>
    /// <param name="resetInterval">Length of each budget window. Must be positive.</param>
    /// <param name="anchor">
    /// Alignment point for the tumbling windows. Defaults to the Unix epoch.
    /// </param>
    public BudgetGroup(
        string id,
        string name,
        TimeSpan allowance,
        TimeSpan resetInterval,
        DateTimeOffset? anchor = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ConfigurationException("Budget group id must not be empty.");
        }

        if (allowance <= TimeSpan.Zero)
        {
            throw new ConfigurationException(
                $"Budget group '{id}' allowance must be positive but was {allowance}.");
        }

        if (resetInterval <= TimeSpan.Zero)
        {
            throw new ConfigurationException(
                $"Budget group '{id}' reset interval must be positive but was {resetInterval}.");
        }

        if (allowance > resetInterval)
        {
            throw new ConfigurationException(
                $"Budget group '{id}' allowance ({allowance}) must not exceed its " +
                $"reset interval ({resetInterval}); otherwise the budget can never be exhausted.");
        }

        Id = id;
        Name = string.IsNullOrWhiteSpace(name) ? id : name;
        Allowance = allowance;
        ResetInterval = resetInterval;
        Anchor = (anchor ?? DateTimeOffset.UnixEpoch).ToUniversalTime();
    }

    public string Id { get; }

    public string Name { get; }

    public TimeSpan Allowance { get; }

    public TimeSpan ResetInterval { get; }

    public DateTimeOffset Anchor { get; }

    /// <summary>
    /// Returns the start of the tumbling budget window that contains
    /// <paramref name="instant"/>.
    /// </summary>
    public DateTimeOffset WindowStartFor(DateTimeOffset instant)
    {
        long delta = (instant.ToUniversalTime() - Anchor).Ticks;
        long index = FloorDiv(delta, ResetInterval.Ticks);
        return Anchor + TimeSpan.FromTicks(index * ResetInterval.Ticks);
    }

    private static long FloorDiv(long a, long b)
    {
        long quotient = a / b;
        long remainder = a % b;
        if (remainder != 0 && (remainder < 0) != (b < 0))
        {
            quotient--;
        }

        return quotient;
    }
}
