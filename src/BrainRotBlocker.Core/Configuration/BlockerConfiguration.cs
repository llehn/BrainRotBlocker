namespace BrainRotBlocker.Core.Configuration;

/// <summary>
/// The validated rule set: the doom-scrolling rules and the budget groups they
/// reference. This is the browser-independent product model; it knows nothing
/// about browsers, the OS, or timers.
/// </summary>
public sealed class BlockerConfiguration
{
    private readonly Dictionary<string, BudgetGroup> _budgetGroups;

    public BlockerConfiguration(
        IReadOnlyList<Rule> rules,
        IReadOnlyList<BudgetGroup> budgetGroups)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(budgetGroups);

        _budgetGroups = new Dictionary<string, BudgetGroup>(StringComparer.Ordinal);
        foreach (BudgetGroup group in budgetGroups)
        {
            if (!_budgetGroups.TryAdd(group.Id, group))
            {
                throw new ConfigurationException(
                    $"Duplicate budget group id '{group.Id}'.");
            }
        }

        var seenRuleIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (Rule rule in rules)
        {
            if (!seenRuleIds.Add(rule.Id))
            {
                throw new ConfigurationException($"Duplicate rule id '{rule.Id}'.");
            }

            foreach (string budgetId in rule.BudgetGroupIds)
            {
                if (!_budgetGroups.ContainsKey(budgetId))
                {
                    throw new ConfigurationException(
                        $"Rule '{rule.Id}' references unknown budget group '{budgetId}'.");
                }
            }
        }

        Rules = rules.ToArray();
        BudgetGroups = _budgetGroups.Values.ToArray();
    }

    public IReadOnlyList<Rule> Rules { get; }

    public IReadOnlyList<BudgetGroup> BudgetGroups { get; }

    public bool TryGetBudgetGroup(string id, out BudgetGroup group)
        => _budgetGroups.TryGetValue(id, out group!);

    /// <summary>Returns every rule whose pattern matches the URL.</summary>
    public IEnumerable<Rule> MatchingRules(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        foreach (Rule rule in Rules)
        {
            if (rule.Matches(uri))
            {
                yield return rule;
            }
        }
    }

    /// <summary>
    /// Returns the distinct budget group ids consumed by the URL: the union of
    /// the budget groups of every rule that matches it.
    /// </summary>
    public ISet<string> BudgetGroupIdsForUrl(Uri uri)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (Rule rule in MatchingRules(uri))
        {
            foreach (string budgetId in rule.BudgetGroupIds)
            {
                result.Add(budgetId);
            }
        }

        return result;
    }
}
