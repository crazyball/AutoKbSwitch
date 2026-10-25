using System.Diagnostics;

namespace KbSwitch;

/// <summary>Listens for foreground-window changes and applies the right keyboard layout.</summary>
internal sealed class Switcher : IDisposable
{
    readonly AppConfig cfg;
    readonly Native.WinEventProc proc;   // kept in a field, otherwise the GC collects it and the hook crashes
    readonly IntPtr hook;
    readonly System.Windows.Forms.Timer retry = new() { Interval = 400 };
    IntPtr lastHwnd;

    public event Action<string, string>? Switched;   // (exe, layout label)

    public Switcher(AppConfig cfg)
    {
        this.cfg = cfg;
        proc = OnForeground;
        hook = Native.SetWinEventHook(Native.EVENT_SYSTEM_FOREGROUND, Native.EVENT_SYSTEM_FOREGROUND,
                                      IntPtr.Zero, proc, 0, 0, Native.WINEVENT_OUTOFCONTEXT);

        // Some games are not ready at the exact moment they get focus: apply once more shortly after.
        retry.Tick += (_, _) =>
        {
            retry.Stop();
            if (Native.GetForegroundWindow() == lastHwnd) Apply(lastHwnd);
        };
    }

    void OnForeground(IntPtr hHook, uint evt, IntPtr hwnd, int idObject, int idChild, uint thread, uint time)
    {
        if (hwnd == IntPtr.Zero) return;
        lastHwnd = hwnd;
        Apply(hwnd);
        retry.Stop();
        retry.Start();
    }

    public void ApplyNow()
    {
        var h = Native.GetForegroundWindow();
        if (h != IntPtr.Zero) Apply(h);
    }

    void Apply(IntPtr hwnd)
    {
        if (!cfg.Enabled) return;

        string exe = ProcessName(hwnd);
        if (exe.Length == 0) return;

        var rule = cfg.Rules.FirstOrDefault(r => string.Equals(r.Process, exe, StringComparison.OrdinalIgnoreCase));
        var lang = Layouts.Find(rule?.Layout)
                   ?? Layouts.Find(cfg.DefaultLayout)
                   ?? InputLanguage.DefaultInputLanguage;

        uint tid = Native.GetWindowThreadProcessId(hwnd, out _);
        if (Native.GetKeyboardLayout(tid) == lang.Handle) return;   // already correct, don't spam

        Native.PostMessage(hwnd, Native.WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, lang.Handle);
        Switched?.Invoke(exe, Layouts.Label(lang));
    }

    public static string ProcessName(IntPtr hwnd)
    {
        Native.GetWindowThreadProcessId(hwnd, out uint pid);
        try
        {
            using var p = Process.GetProcessById((int)pid);
            return p.ProcessName + ".exe";
        }
        catch { return ""; }
    }

    public void Dispose()
    {
        if (hook != IntPtr.Zero) Native.UnhookWinEvent(hook);
        retry.Dispose();
    }
}
