using System.Windows;
using System.Windows.Controls;

namespace Ludo.Presentation.Wpf.Security;

/// <summary>
/// Tooltips and context menus open in their own top-level windows. The UI does not need them, so they
/// are cancelled app-wide rather than risk a frame that escapes capture protection.
/// </summary>
internal static class PopupSuppression
{
    public static void Register()
    {
        EventManager.RegisterClassHandler(
            typeof(FrameworkElement),
            FrameworkElement.ToolTipOpeningEvent,
            new ToolTipEventHandler((_, e) => e.Handled = true));

        // TextBox shows a built-in Cut/Copy/Paste menu even when ContextMenu is null.
        EventManager.RegisterClassHandler(
            typeof(FrameworkElement),
            FrameworkElement.ContextMenuOpeningEvent,
            new ContextMenuEventHandler((_, e) => e.Handled = true));
    }
}
