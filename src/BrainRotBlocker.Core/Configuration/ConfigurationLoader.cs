using System.Text.Json;
using System.Text.Json.Serialization;

namespace BrainRotBlocker.Core.Configuration;

/// <summary>
/// Loads a <see cref="BlockerConfiguration"/> from JSON so the rule set can be
/// changed without recompiling the application (ADR-004).
///
/// Example:
/// <code>
/// {
///   "budgetGroups": [
///     { "id": "short-form-video", "name": "Short-form video",
///       "allowance": "2m", "resetInterval": "1h" }
///   ],
///   "rules": [
///     { "id": "youtube-shorts", "name": "YouTube Shorts",
///       "host": "youtube.com", "pathPrefixes": ["/shorts"],
///       "budgets": ["short-form-video"] }
///   ]
/// }
/// </code>
/// </summary>
public static class ConfigurationLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static BlockerConfiguration Load(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ConfigurationException("Configuration JSON is empty.");
        }

        ConfigurationDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<ConfigurationDto>(json, SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new ConfigurationException($"Configuration JSON is malformed: {ex.Message}", ex);
        }

        if (dto is null)
        {
            throw new ConfigurationException("Configuration JSON deserialized to null.");
        }

        return Map(dto);
    }

    public static BlockerConfiguration LoadFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new ConfigurationException($"Configuration file not found: {path}");
        }

        return Load(File.ReadAllText(path));
    }

    private static BlockerConfiguration Map(ConfigurationDto dto)
    {
        var budgets = new List<BudgetGroup>();
        foreach (BudgetGroupDto group in dto.BudgetGroups ?? new())
        {
            if (string.IsNullOrWhiteSpace(group.Id))
            {
                throw new ConfigurationException("A budget group is missing its 'id'.");
            }

            budgets.Add(new BudgetGroup(
                group.Id,
                group.Name ?? group.Id,
                Duration.Parse(RequireDuration(group.Allowance, group.Id, "allowance")),
                Duration.Parse(RequireDuration(group.ResetInterval, group.Id, "resetInterval")),
                ParseAnchor(group.Anchor, group.Id)));
        }

        var rules = new List<Rule>();
        foreach (RuleDto rule in dto.Rules ?? new())
        {
            if (string.IsNullOrWhiteSpace(rule.Id))
            {
                throw new ConfigurationException("A rule is missing its 'id'.");
            }

            var pattern = new UrlPattern(rule.Host, rule.PathPrefixes, rule.PathRegex);
            rules.Add(new Rule(
                rule.Id,
                rule.Name ?? rule.Id,
                pattern,
                rule.Budgets ?? new List<string>()));
        }

        return new BlockerConfiguration(rules, budgets);
    }

    private static string RequireDuration(string? value, string id, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ConfigurationException($"Budget group '{id}' is missing '{field}'.");
        }

        return value;
    }

    private static DateTimeOffset? ParseAnchor(string? anchor, string id)
    {
        if (string.IsNullOrWhiteSpace(anchor))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(
                anchor,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal,
                out DateTimeOffset parsed))
        {
            return parsed;
        }

        throw new ConfigurationException($"Budget group '{id}' has an invalid 'anchor': {anchor}");
    }

    private sealed class ConfigurationDto
    {
        [JsonPropertyName("budgetGroups")]
        public List<BudgetGroupDto>? BudgetGroups { get; set; }

        [JsonPropertyName("rules")]
        public List<RuleDto>? Rules { get; set; }
    }

    private sealed class BudgetGroupDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Allowance { get; set; }
        public string? ResetInterval { get; set; }
        public string? Anchor { get; set; }
    }

    private sealed class RuleDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Host { get; set; }
        public List<string>? PathPrefixes { get; set; }
        public string? PathRegex { get; set; }
        public List<string>? Budgets { get; set; }
    }
}
