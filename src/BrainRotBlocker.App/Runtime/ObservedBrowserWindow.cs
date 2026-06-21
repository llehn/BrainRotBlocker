namespace BrainRotBlocker.App.Runtime;

internal sealed class ObservedBrowserWindow
{
    public ObservedBrowserWindow(string windowId, IntPtr windowHandle, string browserName, Uri? url)
    {
        WindowId = windowId;
        WindowHandle = windowHandle;
        BrowserName = browserName;
        Url = url;
    }

    public string WindowId { get; }
    public IntPtr WindowHandle { get; }
    public string BrowserName { get; }
    public Uri? Url { get; }
}
