using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ludo.Presentation.Wpf.Security;

/// <summary>
/// Safety net for top-level windows WPF creates behind our back (popups, menus, message boxes):
/// every window this process creates or shows gets capture protection too.
/// </summary>
internal sealed class ProcessWindowGuard : IDisposable
{
    private const uint EventObjectCreate = 0x8000;
    private const uint EventObjectShow = 0x8002;
    private const uint WinEventOutOfContext = 0x0000;
    private const int ObjectIdWindow = 0;
    private const uint GetAncestorRoot = 2;

    private readonly WinEventProc _callback;
    private readonly IntPtr _hook;

    private ProcessWindowGuard()
    {
        // Held in a field so the delegate is not collected while Windows still calls it.
        _callback = OnWinEvent;
        _hook = SetWinEventHook(
            EventObjectCreate,
            EventObjectShow,
            IntPtr.Zero,
            _callback,
            (uint)Environment.ProcessId,
            0,
            WinEventOutOfContext);
    }

    private delegate void WinEventProc(IntPtr hook, uint eventType, IntPtr window, int objectId, int childId, uint thread, uint time);

    public bool IsActive => _hook != IntPtr.Zero;

    /// <summary>Must be called on the UI thread: callbacks are delivered through its message loop.</summary>
    public static ProcessWindowGuard Install() => new();

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            UnhookWinEvent(_hook);
        }
    }

    private void OnWinEvent(IntPtr hook, uint eventType, IntPtr window, int objectId, int childId, uint thread, uint time)
    {
        if (objectId != ObjectIdWindow || window == IntPtr.Zero || GetAncestor(window, GetAncestorRoot) != window)
        {
            return;
        }

        if (!CaptureProtection.IsProtected(window) && CaptureProtection.Apply(window) == ProtectionLevel.None)
        {
            Debug.WriteLine($"Could not protect window 0x{window:X}.");
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr module, WinEventProc callback, uint processId, uint threadId, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWinEvent(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr window, uint flags);
}
