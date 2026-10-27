using System.Windows;
using Ludo.Application.Session;
using Ludo.Presentation.Wpf.Shell;
using Ludo.Presentation.Wpf.Views;

namespace Ludo.Presentation.Wpf.Presenters;

/// <summary>
/// Shows other players' chat lines and game invites in a corner toast while the main window is minimised,
/// hidden in the tray or behind other windows. Bringing the game back to the front, or clicking the toast, dismisses it.
/// </summary>
internal sealed class ChatToastController
{
    private readonly Window _mainWindow;
    private ChatToastWindow? _toast;

    public ChatToastController(Window mainWindow, WpfSessionPresenter presenter)
    {
        _mainWindow = mainWindow;
        presenter.ChatReceived += OnChatReceived;
        presenter.InviteReceived += invite => OnChatReceived(
            new ChatLineView(invite.From, "wants to play Ludo. Open DiceDrop to join in.", IsLocal: false));
        mainWindow.Activated += (_, _) => _toast?.Dismiss();
    }

    private void OnChatReceived(ChatLineView line)
    {
        if (line.IsLocal || _mainWindow.IsActive)
        {
            return;
        }

        if (_toast is null)
        {
            _toast = new ChatToastWindow();
            _toast.MouseLeftButtonUp += (_, _) => BringGameToFront();
        }

        _toast.Push(line);
    }

    private void BringGameToFront()
    {
        _toast?.Dismiss();
        _mainWindow.BringToFront();
    }
}
