using BrainRotBlocker.Core.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BrainRotBlocker.App.Runtime;

/// <summary>
/// The mutable, UI-friendly view of the configuration: a list of rules, each
/// with the sites it blocks and its "when" condition. Round-trips to and from the
/// on-disk JSON schema understood by <see cref="ConfigurationLoader"/>.
/// </summary>
internal sealed class EditableConfiguration
{
    private static readonly DayOfWeek[] AllDays = Enum.GetValues<DayOfWeek>();

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public List<EditableRule> Rules { get; } = new();

    public static EditableConfiguration FromJson(string json)
    {
        ConfigDto dto = JsonSerializer.Deserialize<ConfigDto>(json, ReadOptions) ?? new ConfigDto();
        var result = new EditableConfiguration();
        foreach (RuleDto rule in dto.Rules ?? new())
        {
            result.Rules.Add(EditableRule.FromDto(rule));
        }

        return result;
    }

    public string ToJson()
    {
        var dto = new ConfigDto { Rules = Rules.Select(r => r.ToDto()).ToList() };
        return JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });
    }

    public BlockerConfiguration ToBlockerConfiguration() => ConfigurationLoader.Load(ToJson());

    internal sealed class ConfigDto
    {
        [JsonPropertyName("rules")]
        public List<RuleDto>? Rules { get; set; }
    }

    internal sealed class RuleDto
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

    internal sealed class SiteDto
    {
        public string? Label { get; set; }
        public string? Url { get; set; }
        public bool? IncludeSubpaths { get; set; }
    }

    internal sealed class EditableRule
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";

        public bool BlockCompletely { get; set; }
        public int AllowanceMinutes { get; set; } = 5;

        public bool AllDay { get; set; } = true;
        public TimeOnly From { get; set; } = new(23, 0);
        public TimeOnly To { get; set; } = new(7, 0);
        public HashSet<DayOfWeek> Days { get; set; } = new(AllDays);

        public List<EditableSite> Sites { get; } = new();

        /// <summary>A deep copy, used so the editor can discard unsaved changes.</summary>
        internal EditableRule Clone()
        {
            var copy = new EditableRule
            {
                Id = Id,
                Name = Name,
                BlockCompletely = BlockCompletely,
                AllowanceMinutes = AllowanceMinutes,
                AllDay = AllDay,
                From = From,
                To = To,
                Days = new HashSet<DayOfWeek>(Days),
            };
            foreach (EditableSite site in Sites)
            {
                copy.Sites.Add(new EditableSite
                {
                    Label = site.Label,
                    Url = site.Url,
                    IncludeSubpaths = site.IncludeSubpaths,
                });
            }

            return copy;
        }

        internal static EditableRule FromDto(RuleDto dto)
        {
            var rule = new EditableRule
            {
                Id = dto.Id ?? "",
                Name = dto.Name ?? dto.Id ?? "",
                BlockCompletely = dto.AllowanceMinutes is null,
                AllowanceMinutes = dto.AllowanceMinutes is { } m ? Math.Clamp(m, 1, 59) : 5,
                AllDay = dto.AllDay ?? (dto.From is null && dto.To is null),
                From = ParseTime(dto.From, new TimeOnly(23, 0)),
                To = ParseTime(dto.To, new TimeOnly(7, 0)),
                Days = ParseDays(dto.Days),
            };

            foreach (SiteDto site in dto.Sites ?? new())
            {
                rule.Sites.Add(new EditableSite
                {
                    Label = site.Label ?? site.Url ?? "",
                    Url = site.Url ?? "",
                    IncludeSubpaths = site.IncludeSubpaths ?? true,
                });
            }

            return rule;
        }

        internal RuleDto ToDto() => new()
        {
            Id = Id.Trim(),
            Name = Name.Trim(),
            AllowanceMinutes = BlockCompletely ? null : Math.Clamp(AllowanceMinutes, 1, 59),
            AllDay = AllDay,
            From = AllDay ? null : From.ToString("HH:mm"),
            To = AllDay ? null : To.ToString("HH:mm"),
            Days = Days.Count == AllDays.Length
                ? null
                : AllDays.Where(Days.Contains).Select(d => d.ToString()).ToList(),
            Sites = Sites.Select(s => new SiteDto
            {
                Label = s.Label.Trim(),
                Url = s.Url.Trim(),
                IncludeSubpaths = s.IncludeSubpaths,
            }).ToList(),
        };

        private static TimeOnly ParseTime(string? text, TimeOnly fallback)
            => TimeOnly.TryParse(text, out TimeOnly t) ? t : fallback;

        private static HashSet<DayOfWeek> ParseDays(List<string>? days)
        {
            if (days is not { Count: > 0 })
            {
                return new HashSet<DayOfWeek>(AllDays);
            }

            var set = new HashSet<DayOfWeek>();
            foreach (string day in days)
            {
                if (Enum.TryParse(day, ignoreCase: true, out DayOfWeek parsed))
                {
                    set.Add(parsed);
                }
            }

            return set.Count == 0 ? new HashSet<DayOfWeek>(AllDays) : set;
        }
    }

    internal sealed class EditableSite
    {
        public string Label { get; set; } = "";
        public string Url { get; set; } = "";
        public bool IncludeSubpaths { get; set; } = true;
    }
}
