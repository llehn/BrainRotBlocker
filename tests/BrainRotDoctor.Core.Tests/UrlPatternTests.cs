using BrainRotDoctor.Core.Configuration;
using Xunit;

namespace BrainRotDoctor.Core.Tests;

public class UrlPatternTests
{
    private static Uri U(string s) => new(s, UriKind.Absolute);

    [Theory]
    [InlineData("https://youtube.com/shorts/abc", true)]
    [InlineData("https://www.youtube.com/shorts/abc", true)]
    [InlineData("https://m.youtube.com/shorts/abc", true)]
    [InlineData("https://youtube.com/watch?v=abc", false)]
    [InlineData("https://notyoutube.com/shorts/abc", false)]
    public void HostSuffix_matches_host_and_subdomains_only(string url, bool expected)
    {
        var pattern = new UrlPattern("youtube.com", pathPrefixes: new[] { "/shorts" });
        Assert.Equal(expected, pattern.IsMatch(U(url)));
    }

    [Fact]
    public void Host_only_pattern_matches_any_path()
    {
        var pattern = new UrlPattern("instagram.com");
        Assert.True(pattern.IsMatch(U("https://instagram.com/anything/here")));
    }

    [Theory]
    [InlineData("https://instagram.com/", true)]
    [InlineData("https://instagram.com/reels/", false)]
    public void PathRegex_anchors_match(string url, bool expected)
    {
        var pattern = new UrlPattern("instagram.com", pathRegex: "^/$");
        Assert.Equal(expected, pattern.IsMatch(U(url)));
    }

    [Fact]
    public void Prefix_or_regex_combine_with_or()
    {
        var pattern = new UrlPattern(
            "instagram.com",
            pathPrefixes: new[] { "/reels" },
            pathRegex: "^/$");

        Assert.True(pattern.IsMatch(U("https://instagram.com/reels/x")));
        Assert.True(pattern.IsMatch(U("https://instagram.com/")));
        Assert.False(pattern.IsMatch(U("https://instagram.com/direct/inbox")));
    }

    [Theory]
    [InlineData("about:blank")]
    [InlineData("file:///C:/x.html")]
    [InlineData("ftp://youtube.com/shorts/a")]
    public void Non_http_schemes_never_match(string url)
    {
        var pattern = new UrlPattern("youtube.com");
        Assert.False(pattern.IsMatch(new Uri(url)));
    }

    [Fact]
    public void Path_prefix_is_case_insensitive()
    {
        var pattern = new UrlPattern("youtube.com", pathPrefixes: new[] { "/shorts" });
        Assert.True(pattern.IsMatch(U("https://youtube.com/SHORTS/abc")));
    }

    [Fact]
    public void Empty_pattern_is_rejected()
    {
        Assert.Throws<ConfigurationException>(() => new UrlPattern());
    }

    [Fact]
    public void Invalid_regex_is_rejected()
    {
        Assert.Throws<ConfigurationException>(
            () => new UrlPattern("youtube.com", pathRegex: "([unclosed"));
    }
}
