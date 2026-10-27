using System.Windows;

namespace Ludo.Presentation.Wpf.Shell;

internal static class WindowActivation
{
    /// <summary>Shows the window whether it is hidden in the tray, minimised or behind other windows.</summary>
    public static void BringToFront(this Window window)
    {
        window.Show();
        if (window.WindowState == WindowState.Minimized)
        {
            SystemCommands.RestoreWindow(window);
        }

        window.Activate();
    }
}
