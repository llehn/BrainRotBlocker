using System.Diagnostics;

namespace BrainRotBlocker.App.Runtime;

internal sealed class ProcessProtector : IDisposable
{
    private const string PrimaryMutexName = "Local\\BrainRotBlocker.Primary";
    private const string WatchdogMutexName = "Local\\BrainRotBlocker.Watchdog";
    private static readonly TimeSpan WatchInterval = TimeSpan.FromMilliseconds(650);

    private readonly CancellationTokenSource _stop = new();

    private ProcessProtector()
    {
    }

    public static Mutex AcquirePrimary(out bool ownsMutex) =>
        new(initiallyOwned: true, PrimaryMutexName, out ownsMutex);

    public static void RunWatchdog()
    {
        using Mutex mutex = new(initiallyOwned: true, WatchdogMutexName, out bool ownsMutex);
        if (!ownsMutex)
        {
            return;
        }

        while (true)
        {
            EnsureProcess(PrimaryMutexName, "--role", "primary");
            Thread.Sleep(WatchInterval);
        }
    }

    public static ProcessProtector StartForPrimary(string[] args)
    {
        EnsureProcess(WatchdogMutexName, "--role", "watchdog");

        var protector = new ProcessProtector();
        var thread = new Thread(() => protector.RunPrimaryMonitor())
        {
            IsBackground = true,
            Name = "BrainRotBlocker watchdog monitor",
        };

        thread.Start();
        return protector;
    }

    public void Dispose()
    {
        _stop.Cancel();
    }

    private void RunPrimaryMonitor()
    {
        while (!_stop.IsCancellationRequested)
        {
            EnsureProcess(WatchdogMutexName, "--role", "watchdog");
            _stop.Token.WaitHandle.WaitOne(WatchInterval);
        }
    }

    private static void EnsureProcess(string mutexName, params string[] processArgs)
    {
        using var probe = new Mutex(initiallyOwned: true, mutexName, out bool createdNew);
        if (!createdNew)
        {
            return;
        }

        probe.ReleaseMutex();
        StartSibling(processArgs);
    }

    private static void StartSibling(params string[] args)
    {
        string exe = Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("Cannot resolve the executable path.");
        var info = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
        };

        info.ArgumentList.Add("/d");
        info.ArgumentList.Add("/c");
        info.ArgumentList.Add("start");
        info.ArgumentList.Add("");
        info.ArgumentList.Add(exe);
        foreach (string arg in args)
        {
            info.ArgumentList.Add(arg);
        }

        Process.Start(info);
    }
}
