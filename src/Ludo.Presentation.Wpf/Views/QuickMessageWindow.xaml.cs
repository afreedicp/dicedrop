using System.Windows;
using Ludo.Presentation.Wpf.Security;

namespace Ludo.Presentation.Wpf.Views;

/// <summary>
/// Small compose window opened from the tray, placed above the notification area. Clicking anywhere
/// else closes it, like the tray's own menus.
/// </summary>
public partial class QuickMessageWindow : ProtectedWindow
{
    private const double ScreenMargin = 16;
    private bool _closing;

    public QuickMessageWindow()
    {
        InitializeComponent();
        SizeChanged += (_, _) => PlaceInCorner();
        Loaded += (_, _) =>
        {
            Activate();
            Composer.Focus();
        };
        Deactivated += (_, _) => CloseOnce();
        ProtectionFailed += (_, _) => CloseOnce();
    }

    public void CloseOnce()
    {
        if (!_closing)
        {
            _closing = true;
            Close();
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _closing = true;
        base.OnClosing(e);
    }

    private void PlaceInCorner()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - ActualWidth - ScreenMargin;
        Top = area.Bottom - ActualHeight - ScreenMargin;
    }
}
