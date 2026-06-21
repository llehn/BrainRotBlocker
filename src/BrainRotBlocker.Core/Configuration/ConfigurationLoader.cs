using System.Text.Json;
using System.Text.Json.Serialization;

namespace BrainRotBlocker.Core.Configuration;

/// <summary>
/// Loads a <see cref="BlockerConfiguration"/> from JSON so the rule set can be
/// changed without recompiling the application.
///
/// Example:
/// <code>
/// {
///   "rules": [
///     {
///       "id": "short-video",
///       "name": "Short video",
///       "allowanceMinutes": 5,        // omit to block completely
///       "allDay": true,               // or false with "from"/"to"
///       "from": "23:00", "to": "07:00",
///       "days": ["Monday", "Tuesday"],// omit for every day
///       "sites": [
///         { "label": "Instagram Reels", "url": "instagram.com/reels",
///           "includeSubpaths": true }
///       ]
///     }
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

        var rules = new List<Rule>();
        foreach (RuleDto rule in dto.Rules ?? new())
        {
            rules.Add(MapRule(rule));
        }

        return new BlockerConfiguration(rules);
    }

    public static BlockerConfiguration LoadFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new ConfigurationException($"Configuration file not found: {path}");
        }

        return Load(File.ReadAllText(path));
    }

    private static Rule MapRule(RuleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Id))
        {
            throw new ConfigurationException("A rule is missing its 'id'.");
        }

        var sites = new List<TargetSite>();
        foreach (SiteDto site in dto.Sites ?? new())
        {
            if (string.IsNullOrWhiteSpace(site.Url))
            {
                throw new ConfigurationException($"Rule '{dto.Id}' has a site with no 'url'.");
            }

            UrlPattern pattern = SiteUrl.ToPattern(site.Url, site.IncludeSubpaths ?? true);
            sites.Add(new TargetSite(site.Label ?? site.Url, pattern));
        }

        TimeSpan? allowance = dto.AllowanceMinutes is { } minutes
            ? TimeSpan.FromMinutes(minutes)
            : null;

        bool allDay = dto.AllDay ?? (dto.From is null && dto.To is null);

        return new Rule(
            dto.Id,
            dto.Name ?? dto.Id,
            sites,
            allowance,
            allDay,
            ParseTime(dto.From, dto.Id, "from"),
            ParseTime(dto.To, dto.Id, "to"),
            ParseDays(dto.Days, dto.Id));
    }

    private static TimeOnly ParseTime(string? value, string id, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new TimeOnly(0, 0);
        }

        if (TimeOnly.TryParse(
                value,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out TimeOnly parsed))
        {
            return parsed;
        }

        throw new ConfigurationException(
            $"Rule '{id}' has an invalid '{field}' time '{value}'. Use a form like '23:00'.");
    }

    private static IReadOnlyCollection<DayOfWeek>? ParseDays(List<string>? days, string id)
    {
        if (days is not { Count: > 0 })
        {
            return null;
        }

        var result = new List<DayOfWeek>();
        foreach (string day in days)
        {
            if (Enum.TryParse(day, ignoreCase: true, out DayOfWeek parsed))
            {
                result.Add(parsed);
            }
            else
            {
                throw new ConfigurationException($"Rule '{id}' has an invalid day '{day}'.");
            }
        }

        return result;
    }

    private sealed class ConfigurationDto
    {
        [JsonPropertyName("rules")]
        public List<RuleDto>? Rules { get; set; }
    }

    private sealed class RuleDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public int? AllowanceMinutes { get; set; }
        public bool? AllDay { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
        public List<string>? Days { get; set; }
        public List<SiteDto>? Sites { get; set; }
    }

    private sealed class SiteDto
    {
        public string? Label { get; set; }
        public string? Url { get; set; }
        public bool? IncludeSubpaths { get; set; }
    }
}
