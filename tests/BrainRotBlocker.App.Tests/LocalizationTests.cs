using BrainRotBlocker.App.Ui;
using System.Text.RegularExpressions;
using Xunit;

namespace BrainRotBlocker.App.Tests;

public sealed class LocalizationTests
{
    private static readonly Dictionary<string, Dictionary<string, string>> Tables = Strings.BuildTables();
    private static readonly Dictionary<string, string> English = Tables["en"];

    public static IEnumerable<object[]> Codes => Tables.Keys.Select(c => new object[] { c });

    [Theory]
    [MemberData(nameof(Codes))]
    public void Every_language_has_all_english_keys(string code)
    {
        string[] missing = English.Keys.Where(k => !Tables[code].ContainsKey(k)).ToArray();
        Assert.True(missing.Length == 0, $"{code} is missing: {string.Join(", ", missing)}");
    }

    [Theory]
    [MemberData(nameof(Codes))]
    public void Every_language_uses_the_same_placeholders_as_english(string code)
    {
        foreach ((string key, string english) in English)
        {
            if (!Tables[code].TryGetValue(key, out string? translated))
            {
                continue;
            }

            Assert.True(
                Placeholders(english).SetEquals(Placeholders(translated)),
                $"{code}/{key}: placeholders differ from English ('{translated}').");
        }
    }

    [Fact]
    public void Every_listed_language_has_a_table()
    {
        foreach ((string code, _) in Loc.Languages)
        {
            Assert.True(Tables.ContainsKey(code), $"No translation table for listed language '{code}'.");
        }
    }

    [Fact]
    public void Catalog_of_languages_is_unique()
    {
        string[] codes = Loc.Languages.Select(l => l.Code).ToArray();
        Assert.Equal(codes.Length, codes.Distinct().Count());
    }

    private static HashSet<string> Placeholders(string text) =>
        Regex.Matches(text, @"\{(\d+)\}").Select(m => m.Value).ToHashSet();
}
