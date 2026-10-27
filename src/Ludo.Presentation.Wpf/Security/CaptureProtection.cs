using System.Runtime.InteropServices;

namespace Ludo.Presentation.Wpf.Security;

internal enum ProtectionLevel
{
    None,

    /// <summary>Window shows up black in captures (pre-2004 Windows 10 fallback).</summary>
    Blackout,

    /// <summary>Window is left out of captures entirely (Windows 10 2004+).</summary>
    Excluded,
}

/// <summary>
/// Wraps SetWindowDisplayAffinity. With WDA_EXCLUDEFROMCAPTURE, DWM leaves the window out of
/// PrintScreen, Snipping Tool, Game Bar, OBS, Teams/Zoom/Discord sharing, Graphics Capture and
/// desktop duplication.
/// </summary>
internal static class CaptureProtection
{
    private const uint WdaNone = 0x00;
    private const uint WdaMonitor = 0x01;
    private const uint WdaExcludeFromCapture = 0x11;

    public static ProtectionLevel Apply(IntPtr window)
    {
        if (window == IntPtr.Zero)
        {
            return ProtectionLevel.None;
        }

        if (SetWindowDisplayAffinity(window, WdaExcludeFromCapture) && Current(window) == WdaExcludeFromCapture)
        {
            return ProtectionLevel.Excluded;
        }

        if (SetWindowDisplayAffinity(window, WdaMonitor) && Current(window) == WdaMonitor)
        {
            return ProtectionLevel.Blackout;
        }

        return ProtectionLevel.None;
    }

    public static bool IsProtected(IntPtr window) => Current(window) != WdaNone;

    private static uint Current(IntPtr window) =>
        GetWindowDisplayAffinity(window, out var affinity) ? affinity : WdaNone;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowDisplayAffinity(IntPtr hWnd, out uint pdwAffinity);
}
