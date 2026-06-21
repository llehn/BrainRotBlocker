namespace BrainRotBlocker.App.Runtime;

internal interface IBrowserTabCloser
{
    bool CloseSelectedTab(IntPtr windowHandle);
}
