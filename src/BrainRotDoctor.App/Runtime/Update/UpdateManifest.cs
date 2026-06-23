using System.Text.Json;
using System.Text.Json.Serialization;

namespace BrainRotDoctor.App.Runtime.Update;

/// <summary>
/// The signed <c>update.json</c> published alongside each release. Its SHA-256
/// transitively protects the (large) exe asset, so only this small file is
/// signed. <see cref="Parse"/> rejects anything malformed or incomplete.
/// </summary>
internal sealed class UpdateManifest
{
    [JsonPropertyName("version")]
    public string Version { get; init; } = "";

    /// <summary>The release asset file name of the exe to download.</summary>
    [JsonPropertyName("asset")]
    public string Asset { get; init; } = "";

    /// <summary>Lowercase hex SHA-256 of the exe asset.</summary>
    [JsonPropertyName("sha256")]
    public string Sha256 { get; init; } = "";

    [JsonPropertyName("size")]
    public long Size { get; init; }

    /// <summary>The parsed semantic <see cref="Version"/>, or null if unparseable.</summary>
    public Version? SemanticVersion =>
        System.Version.TryParse(Version, out Version? v) ? v : null;

    /// <summary>
    /// Parses and validates a manifest from its raw bytes. Returns null when the
    /// JSON is malformed or any required field is missing/invalid, so callers can
    /// treat a bad manifest the same as no update.
    /// </summary>
    public static UpdateManifest? Parse(byte[] json)
    {
        UpdateManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<UpdateManifest>(json);
        }
        catch (JsonException)
        {
            return null;
        }

        if (manifest is null
            || manifest.SemanticVersion is null
            || string.IsNullOrWhiteSpace(manifest.Asset)
            || !IsHexSha256(manifest.Sha256))
        {
            return null;
        }

        return manifest;
    }

    private static bool IsHexSha256(string value)
    {
        if (value.Length != 64)
        {
            return false;
        }

        foreach (char c in value)
        {
            bool isHex = c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';
            if (!isHex)
            {
                return false;
            }
        }

        return true;
    }
}
