using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Ludo.Presentation.Wpf.Security;

namespace Ludo.Presentation.Wpf.Views;

/// <summary>
/// Small notice in the bottom-right corner for chat that arrives while the game is in the background.
/// It is a <see cref="ProtectedWindow"/> like the main window, so it never shows up in captures either.
/// </summary>
public partial class ChatToastWindow : ProtectedWindow
{
    private const int MaxLines = 3;
    private const double ScreenMargin = 16;
    private const int GwlExStyle = -20;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(6);

    private readonly ObservableCollection<ToastLine> _lines = [];
    private readonly DispatcherTimer _hideTimer;
    private bool _protectionFailed;

    public ChatToastWindow()
    {
        InitializeComponent();
        Lines.ItemsSource = _lines;
        _hideTimer = new DispatcherTimer { Interval = Lifetime };
        _hideTimer.Tick += (_, _) => Dismiss();
        SizeChanged += (_, _) => PlaceInCorner();
        ProtectionFailed += (_, _) => _protectionFailed = true;
    }

    public void Push(ToastLine line)
    {
        if (_protectionFailed)
        {
            return;
        }

        _lines.Add(line);
        while (_lines.Count > MaxLines)
        {
            _lines.RemoveAt(0);
        }

        _hideTimer.Stop();
        _hideTimer.Start();
        if (!IsVisible)
        {
            Show();
            SlideIn();
        }
    }

    public void Dismiss()
    {
        _hideTimer.Stop();
        _lines.Clear();
        Hide();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Keep it out of Alt+Tab and never let it take focus from whatever the user is doing.
        var handle = new WindowInteropHelper(this).Handle;
        SetWindowLong(handle, GwlExStyle, GetWindowLong(handle, GwlExStyle) | WsExToolWindow | WsExNoActivate);
    }

    /// <summary>The content glides in from the screen edge when the toast appears.</summary>
    private void SlideIn()
    {
        if (!SystemParameters.ClientAreaAnimation)
        {
            return;
        }

        var duration = TimeSpan.FromMilliseconds(320);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var shift = new TranslateTransform();
        Body.RenderTransform = shift;
        shift.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(40, 0, duration) { EasingFunction = ease });
        Body.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, duration) { EasingFunction = ease });
    }

    private void PlaceInCorner()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - ActualWidth - ScreenMargin;
        Top = area.Bottom - ActualHeight - ScreenMargin;
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(IntPtr window, int index, int value);
}

/// <param name="Color">Avatar colour; null falls back to the accent.</param>
public sealed record ToastLine(string Initials, Brush? Color, ImageSource? Photo, string Title, string Text);
