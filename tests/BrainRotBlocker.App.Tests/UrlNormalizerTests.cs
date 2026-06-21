using BrainRotBlocker.App.Runtime;
using Xunit;

namespace BrainRotBlocker.App.Tests;

public sealed class UrlNormalizerTests
{
    [Theory]
    [InlineData("https://instagram.com/reels/abc", "https://instagram.com/reels/abc")]
    [InlineData("instagram.com/reels/abc", "https://instagram.com/reels/abc")]
    [InlineData("www.youtube.com/shorts/xyz", "https://www.youtube.com/shorts/xyz")]
    [InlineData("  https://www.tiktok.com/foryou  ", "https://www.tiktok.com/foryou")]
    public void Normalizes_browser_address_bar_urls(string input, string expected)
    {
        Assert.True(UrlNormalizer.TryNormalize(input, out Uri? uri));
        Assert.NotNull(uri);
        Assert.Equal(expected, uri.AbsoluteUri.TrimEnd('/'));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Search or enter address")]
    [InlineData("instagram reels")]
    [InlineData("about:blank")]
    [InlineData("file:///C:/tmp/test.html")]
    public void Rejects_non_web_url_values(string input)
    {
        Assert.False(UrlNormalizer.TryNormalize(input, out Uri? uri));
        Assert.Null(uri);
    }
}
