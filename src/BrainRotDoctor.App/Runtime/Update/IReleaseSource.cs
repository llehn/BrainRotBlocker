namespace BrainRotDoctor.App.Runtime.Update;

/// <summary>One downloadable file attached to a release.</summary>
internal sealed record ReleaseAsset(string Name, string DownloadUrl);

/// <summary>The latest published release and its assets.</summary>
internal sealed record ReleaseInfo(string Tag, IReadOnlyList<ReleaseAsset> Assets)
{
    public ReleaseAsset? FindAsset(string name) =>
        Assets.FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Abstracts where releases come from (GitHub in production) so the update logic
/// can be tested without the network.
/// </summary>
internal interface IReleaseSource
{
    /// <summary>The latest release, or null if none/unavailable.</summary>
    Task<ReleaseInfo?> GetLatestReleaseAsync(CancellationToken cancellationToken);

    /// <summary>Downloads an asset's bytes (used for the small manifest + signature).</summary>
    Task<byte[]> DownloadBytesAsync(string url, CancellationToken cancellationToken);

    /// <summary>Streams an asset to <paramref name="destinationPath"/> (used for the exe).</summary>
    Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken);
}
