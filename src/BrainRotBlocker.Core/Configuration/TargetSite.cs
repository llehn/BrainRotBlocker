namespace BrainRotBlocker.Core.Configuration;

/// <summary>
/// One website a rule blocks: a friendly label plus the URL the user typed,
/// compiled into a <see cref="UrlPattern"/>. The same site may appear in several
/// rules.
/// </summary>
public sealed class TargetSite
{
    /// <param name="label">Human-readable label for display.</param>
    /// <param name="pattern">URL pattern identifying the site.</param>
    public TargetSite(string label, UrlPattern pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        Label = string.IsNullOrWhiteSpace(label) ? pattern.HostSuffix ?? "site" : label;
        Pattern = pattern;
    }

    public string Label { get; }

    public UrlPattern Pattern { get; }

    public bool Matches(Uri uri) => Pattern.IsMatch(uri);
}
