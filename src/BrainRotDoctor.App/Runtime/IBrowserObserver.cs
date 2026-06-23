namespace BrainRotDoctor.App.Runtime;

internal interface IBrowserObserver
{
    IReadOnlyList<ObservedBrowserWindow> GetSelectedTabs();
}
