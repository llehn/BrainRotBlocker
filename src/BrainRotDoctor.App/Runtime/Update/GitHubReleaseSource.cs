using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace BrainRotDoctor.App.Runtime.Update;

/// <summary>
/// Reads the latest release from the GitHub REST API and downloads its assets.
/// Uses the public, unauthenticated endpoint (rate limit is ample for a check
/// every few hours).
/// </summary>
internal sealed class GitHubReleaseSource : IReleaseSource
{
    private readonly HttpClient _http;
    private readonly string _owner;
    private readonly string _repo;

    public GitHubReleaseSource(string owner, string repo, HttpClient http)
    {
        _owner = owner;
        _repo = repo;
        _http = http;
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
        {
            _http.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("BrainRotDoctor", AppVersion.Current.ToString()));
        }
    }

    public async Task<ReleaseInfo?> GetLatestReleaseAsync(CancellationToken cancellationToken)
    {
        string url = $"https://api.github.com/repos/{_owner}/{_repo}/releases/latest";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using Stream content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using JsonDocument doc = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken).ConfigureAwait(false);
        JsonElement root = doc.RootElement;

        if (root.TryGetProperty("draft", out JsonElement draft) && draft.GetBoolean())
        {
            return null;
        }

        if (!root.TryGetProperty("tag_name", out JsonElement tagElement) || tagElement.GetString() is not { } tag)
        {
            return null;
        }

        var assets = new List<ReleaseAsset>();
        if (root.TryGetProperty("assets", out JsonElement assetsElement)
            && assetsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement asset in assetsElement.EnumerateArray())
            {
                if (asset.TryGetProperty("name", out JsonElement nameElement)
                    && asset.TryGetProperty("browser_download_url", out JsonElement urlElement)
                    && nameElement.GetString() is { } name
                    && urlElement.GetString() is { } downloadUrl)
                {
                    assets.Add(new ReleaseAsset(name, downloadUrl));
                }
            }
        }

        return new ReleaseInfo(tag, assets);
    }

    public async Task<byte[]> DownloadBytesAsync(string url, CancellationToken cancellationToken) =>
        await _http.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);

    public async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken)
    {
        string? dir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using HttpResponseMessage response = await _http
            .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var destination = new FileStream(
            destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
    }
}
