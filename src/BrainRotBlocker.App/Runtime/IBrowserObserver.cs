namespace BrainRotBlocker.App.Runtime;

internal interface IBrowserObserver
{
    IReadOnlyList<ObservedBrowserWindow> GetSelectedTabs();
}
