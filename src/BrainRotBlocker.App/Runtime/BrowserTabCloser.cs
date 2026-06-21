namespace BrainRotBlocker.App.Runtime;

internal sealed class BrowserTabCloser : IBrowserTabCloser
{
    public bool CloseSelectedTab(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero || !NativeMethods.IsWindow(windowHandle))
        {
            return false;
        }

        NativeMethods.ShowWindow(windowHandle, NativeMethods.SW_RESTORE);
        NativeMethods.SetForegroundWindow(windowHandle);
        Thread.Sleep(80);

        NativeMethods.KeyDown(NativeMethods.VK_CONTROL);
        NativeMethods.KeyDown(NativeMethods.VK_W);
        NativeMethods.KeyUp(NativeMethods.VK_W);
        NativeMethods.KeyUp(NativeMethods.VK_CONTROL);
        Thread.Sleep(120);

        return true;
    }
}
