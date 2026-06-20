namespace BrainRotBlocker.Core.Configuration;

/// <summary>
/// A configured doom-scrolling surface: a URL pattern plus the budget groups it
/// consumes. A rule may consume several budgets, and a budget may be shared by
/// several rules (the N-to-M relationship from ADR-004).
/// </summary>
public sealed class Rule
{
    /// <param name="id">Stable identifier for the rule.</param>
    /// <param name="name">Human-readable name for display.</param>
    /// <param name="pattern">URL pattern identifying the surface.</param>
    /// <param name="budgetGroupIds">
    /// Ids of the budget groups this surface consumes. Must be non-empty.
    /// </param>
    public Rule(
        string id,
        string name,
        UrlPattern pattern,
        IReadOnlyList<string> budgetGroupIds)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ConfigurationException("Rule id must not be empty.");
        }

        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(budgetGroupIds);

        if (budgetGroupIds.Count == 0)
        {
            throw new ConfigurationException(
                $"Rule '{id}' must reference at least one budget group.");
        }

        if (budgetGroupIds.Any(string.IsNullOrWhiteSpace))
        {
            throw new ConfigurationException(
                $"Rule '{id}' references an empty budget group id.");
        }

        Id = id;
        Name = string.IsNullOrWhiteSpace(name) ? id : name;
        Pattern = pattern;
        BudgetGroupIds = budgetGroupIds.Distinct(StringComparer.Ordinal).ToArray();
    }

    public string Id { get; }

    public string Name { get; }

    public UrlPattern Pattern { get; }

    public IReadOnlyList<string> BudgetGroupIds { get; }

    /// <summary>Returns true if the given URL is this doom-scrolling surface.</summary>
    public bool Matches(Uri uri) => Pattern.IsMatch(uri);
}
