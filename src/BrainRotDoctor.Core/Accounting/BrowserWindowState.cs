namespace BrainRotDoctor.Core.Accounting;

/// <summary>
/// A snapshot of one open browser window: its stable identity and the URL of
/// its selected tab / current page. This is the only browser-derived input the
/// core model consumes; how it is obtained (UI Automation, an adapter, a test)
/// is outside the model.
///
/// Per ADR-005 only the selected tab of each window is represented. A window
/// whose current page has no URL (new tab page, malformed) carries a null
/// <see cref="Url"/> and never matches a rule.
/// </summary>
public sealed class BrowserWindowState
{
    public BrowserWindowState(string windowId, Uri? url)
    {
        if (string.IsNullOrWhiteSpace(windowId))
        {
            throw new ArgumentException("Window id must not be empty.", nameof(windowId));
        }

        WindowId = windowId;
        Url = url;
    }

    /// <summary>
    /// Identity that is stable for the lifetime of the browser window, used to
    /// target a close action. For UI Automation this is typically the native
    /// window handle rendered as a string.
    /// </summary>
    public string WindowId { get; }

    /// <summary>URL of the selected tab / current page, or null if none.</summary>
    public Uri? Url { get; }

    /// <summary>Convenience factory that parses a string URL, tolerating junk.</summary>
    public static BrowserWindowState FromString(string windowId, string? url)
    {
        Uri? parsed = null;
        if (!string.IsNullOrWhiteSpace(url)
            && Uri.TryCreate(url, UriKind.Absolute, out Uri? u))
        {
            parsed = u;
        }

        return new BrowserWindowState(windowId, parsed);
    }
}
