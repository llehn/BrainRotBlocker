using BrainRotBlocker.Core.Accounting;

namespace BrainRotBlocker.App.Runtime;

internal sealed class AppStatus
{
    public AppStatus(
        DateTimeOffset updatedAt,
        bool isRunning,
        string configSource,
        IReadOnlyList<ObservedBrowserWindow> windows,
        IReadOnlyList<RuleSnapshot> rules,
        IReadOnlyList<CloseEvent> recentClosures,
        StrictModeSnapshot strictMode,
        string? lastError)
    {
        UpdatedAt = updatedAt;
        IsRunning = isRunning;
        ConfigSource = configSource;
        Windows = windows;
        Rules = rules;
        RecentClosures = recentClosures;
        StrictMode = strictMode;
        LastError = lastError;
    }

    public DateTimeOffset UpdatedAt { get; }
    public bool IsRunning { get; }
    public string ConfigSource { get; }
    public IReadOnlyList<ObservedBrowserWindow> Windows { get; }
    public IReadOnlyList<RuleSnapshot> Rules { get; }
    public IReadOnlyList<CloseEvent> RecentClosures { get; }
    public StrictModeSnapshot StrictMode { get; }
    public string? LastError { get; }
}

internal sealed class CloseEvent
{
    public CloseEvent(DateTimeOffset closedAt, string browser, Uri url, string ruleId)
    {
        ClosedAt = closedAt;
        Browser = browser;
        Url = url;
        RuleId = ruleId;
    }

    public DateTimeOffset ClosedAt { get; }
    public string Browser { get; }
    public Uri Url { get; }
    public string RuleId { get; }
}
