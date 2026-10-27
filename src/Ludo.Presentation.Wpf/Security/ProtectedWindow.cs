using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Ludo.Presentation.Wpf.Security;

/// <summary>
/// A window that is capture-protected before it is ever shown and fails closed: if protection cannot
/// be applied, or is stripped later and cannot be restored, the content is removed and the window closes.
/// </summary>
public class ProtectedWindow : Window
{
    private static readonly TimeSpan WatchdogInterval = TimeSpan.FromMilliseconds(200);

    private readonly DispatcherTimer _watchdog;
    private IntPtr _handle;

    public ProtectedWindow()
    {
        _watchdog = new DispatcherTimer(DispatcherPriority.Send) { Interval = WatchdogInterval };
        _watchdog.Tick += (_, _) => Enforce();
    }

    public event EventHandler? ProtectionFailed;

    internal ProtectionLevel Level { get; private set; }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // The HWND exists but has not been shown or painted yet, so no frame is ever unprotected.
        _handle = new WindowInteropHelper(this).Handle;
        Level = CaptureProtection.Apply(_handle);
        if (Level == ProtectionLevel.None)
        {
            FailClosed();
            return;
        }

        _watchdog.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        _watchdog.Stop();
        base.OnClosed(e);
    }

    private void Enforce()
    {
        if (CaptureProtection.IsProtected(_handle))
        {
            return;
        }

        // Something cleared our affinity (e.g. injected code). Hide first, then try to restore.
        var content = Content;
        Content = null;
        Level = CaptureProtection.Apply(_handle);
        if (Level == ProtectionLevel.None)
        {
            FailClosed();
            return;
        }

        Content = content;
    }

    private void FailClosed()
    {
        _watchdog.Stop();
        Content = null;
        Hide();
        ProtectionFailed?.Invoke(this, EventArgs.Empty);
    }
}
