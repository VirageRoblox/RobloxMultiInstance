using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace TinyAcc;

public partial class App : Application
{
    // Hold the Roblox singleton name as a MUTEX. Roblox expects an EVENT of this
    // name and uses it to tell the old client to close when a new game launches.
    // Claiming the name as the wrong object type makes Roblox's event code fail,
    // which disables that "close the other instance" behavior — so multiple
    // clients coexist. This only works if we own the name BEFORE any Roblox is
    // running (otherwise the event already exists and we can't claim it).
    private const string SingletonName = "ROBLOX_singletonEvent";

    private static readonly string[] GameProcessNames =
        { "RobloxPlayerBeta", "RobloxPlayerLauncher", "Windows10Universal" };

    private Mutex? _mutex;

    /// <summary>True while we hold the singleton (multi-instance enabled).</summary>
    public bool Active { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        TryAcquire();
    }

    /// <summary>
    /// Attempts to claim the singleton name. Succeeds only when no Roblox owns it
    /// yet. Safe to call repeatedly (e.g. from a Retry button after closing Roblox).
    /// </summary>
    public bool TryAcquire()
    {
        if (Active) return true;
        try
        {
            _mutex = new Mutex(true, SingletonName, out _);
            Active = true;
        }
        catch
        {
            // WaitHandleCannotBeOpenedException: the name already exists as an
            // Event -> Roblox is currently running. Close it and retry.
            _mutex = null;
            Active = false;
        }
        return Active;
    }

    /// <summary>
    /// True when a game client is running IN THIS Windows session. Even if we
    /// grabbed the mutex, a client that launched first owns the real singleton
    /// event — multi-instance is only reliable when TinyAcc starts on a clean
    /// slate, so the UI warns and asks for a close + retry.
    ///
    /// Session-scoped on purpose: process enumeration is system-wide, so without
    /// the SessionId filter, TinyAcc on your console desktop would see (and try to
    /// close) Roblox running inside a Remote Desktop session, and vice-versa. Each
    /// session's multi-instance is independent, so each TinyAcc must mind only its own.
    /// </summary>
    public static bool IsGameRunning()
    {
        int sid = CurrentSessionId();
        foreach (var name in GameProcessNames)
            foreach (var p in Process.GetProcessesByName(name))
            {
                bool ours;
                try { ours = p.SessionId == sid; } catch { ours = false; }
                p.Dispose();
                if (ours) return true;
            }
        return false;
    }

    /// <summary>Closes game clients in THIS session only (never touches other sessions).</summary>
    public static void CloseGamesInSession()
    {
        int sid = CurrentSessionId();
        foreach (var name in GameProcessNames)
            foreach (var p in Process.GetProcessesByName(name))
            {
                try { if (p.SessionId == sid) p.Kill(); } catch { }
                finally { p.Dispose(); }
            }
    }

    private static int CurrentSessionId()
    {
        using var me = Process.GetCurrentProcess();
        return me.SessionId;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { _mutex?.ReleaseMutex(); } catch { }
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
