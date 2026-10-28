using System.Drawing;
using System.Windows;
using Ludo.Application.Session;
using Ludo.Presentation.Wpf.Presenters;
using Forms = System.Windows.Forms;

namespace Ludo.Presentation.Wpf.Shell;

/// <summary>
/// Keeps the game running in the notification area. Closing the window only hides it; the tray menu opens
/// it, sends a quick message, toggles start with Windows, or exits. A game that starts while we are hidden brings the window
/// back, since the other players would otherwise wait on our turns.
/// </summary>
internal sealed class TrayShell : IDisposable
{
    private readonly Window _mainWindow;
    private readonly Action<int> _exit;
    private readonly Forms.NotifyIcon _icon;
    private readonly Forms.ContextMenuStrip _menu;
    private readonly Forms.ToolStripMenuItem _startWithWindows;
    private Guid? _lastGame;
    private bool _exiting;
    private bool _hintShown;

    /// <param name="sendMessage">Opens the quick compose window.</param>
    public TrayShell(Window mainWindow, WpfSessionPresenter presenter, Action<int> exit, Action sendMessage)
    {
        _mainWindow = mainWindow;
        _exit = exit;

        var open = new Forms.ToolStripMenuItem("Open DiceDrop", null, (_, _) => _mainWindow.BringToFront());
        open.Font = new Font(open.Font, System.Drawing.FontStyle.Bold);
        var compose = new Forms.ToolStripMenuItem("Send message…", null, (_, _) => sendMessage());
        _startWithWindows = new Forms.ToolStripMenuItem("Start with Windows", null, (_, _) => ToggleStartWithWindows());
        _menu = new Forms.ContextMenuStrip();
        _menu.Items.AddRange([open, compose, new Forms.ToolStripSeparator(), _startWithWindows, new Forms.ToolStripSeparator(), new Forms.ToolStripMenuItem("Exit", null, (_, _) => Exit())]);
        _menu.Opening += (_, _) => _startWithWindows.Checked = StartupRegistration.IsEnabled;

        _icon = new Forms.NotifyIcon { Icon = LoadIcon(), Text = "DiceDrop", ContextMenuStrip = _menu, Visible = true };
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left)
            {
                _mainWindow.BringToFront();
            }
        };

        mainWindow.Closing += OnMainWindowClosing;
        presenter.SessionChanged += OnSessionChanged;

        // Logging off must not be blocked by close-to-tray.
        System.Windows.Application.Current.SessionEnding += (_, _) => _exiting = true;
    }

    public void Exit(int exitCode = 0)
    {
        _exiting = true;
        _exit(exitCode);
    }

    public void Dispose()
    {
        // Without this the icon lingers in the tray until the mouse passes over it.
        _icon.Visible = false;
        _icon.Dispose();
        _menu.Dispose();
    }

    private void OnMainWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_exiting)
        {
            return;
        }

        e.Cancel = true;
        _mainWindow.Hide();
        if (!_hintShown)
        {
            _hintShown = true;
            _icon.ShowBalloonTip(4000, "DiceDrop is still running", "Open it or exit from the tray icon.", Forms.ToolTipIcon.None);
        }
    }

    private void OnSessionChanged(SessionSnapshot snapshot)
    {
        // A game we are playing in just began: don't leave it waiting on us in the tray.
        var gameStarted = snapshot.Match is { IsPlaying: true, Phase: MatchPhaseView.Playing } match && match.Id != _lastGame;
        if (gameStarted)
        {
            _lastGame = snapshot.Match!.Id;
        }

        if (gameStarted && (!_mainWindow.IsVisible || _mainWindow.WindowState == WindowState.Minimized))
        {
            _mainWindow.BringToFront();
        }
    }

    private void ToggleStartWithWindows()
    {
        try
        {
            StartupRegistration.SetEnabled(!StartupRegistration.IsEnabled);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or System.IO.IOException)
        {
            _icon.ShowBalloonTip(4000, "Could not change start with Windows", ex.Message, Forms.ToolTipIcon.Warning);
        }
    }

    private static Icon LoadIcon()
    {
        try
        {
            return Environment.ProcessPath is { } path ? Icon.ExtractAssociatedIcon(path) ?? SystemIcons.Application : SystemIcons.Application;
        }
        catch (ArgumentException)
        {
            return SystemIcons.Application;
        }
    }
}
