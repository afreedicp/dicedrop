using System.Windows.Input;
using Ludo.Application.Session;

namespace Ludo.Presentation.Wpf.ViewModels;

/// <summary>
/// The tray's "Send message" window: pick the lobby or a person, type, send. It closes after sending,
/// so a quick reply never needs the main window.
/// </summary>
public sealed class QuickMessageViewModel : ObservableObject
{
    private readonly AsyncCommand _sendCommand;
    private ConversationViewModel? _target;
    private string _text = string.Empty;

    internal QuickMessageViewModel(Ludo.Application.LudoApplication app, LobbyViewModel lobby, Action<Exception> onError, Action close)
    {
        Targets = lobby.WritableConversations().ToArray();
        _target = Targets.FirstOrDefault();
        _sendCommand = new AsyncCommand(
            async _ =>
            {
                var target = Target!;
                await (target.Person is null ? app.SendChat.ExecuteAsync(Text) : app.SendDirectChat.ExecuteAsync(target.Person.Key, Text));
                Text = string.Empty;
                close();
            },
            onError,
            _ => Target is not null && !string.IsNullOrWhiteSpace(Text));
        PickCommand = new AsyncCommand(target => { Target = target as ConversationViewModel; return Task.CompletedTask; }, onError);
        CloseCommand = new AsyncCommand(_ => { close(); return Task.CompletedTask; }, onError);
    }

    /// <summary>The lobby chat, then everyone who is still in the lobby.</summary>
    public IReadOnlyList<ConversationViewModel> Targets { get; }

    public bool HasTargets => Targets.Count > 0;

    public ConversationViewModel? Target
    {
        get => _target;
        private set
        {
            if (SetProperty(ref _target, value))
            {
                OnPropertyChanged(nameof(Placeholder));
                _sendCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Placeholder => Target?.Placeholder ?? "Not in a lobby yet";

    public string Text
    {
        get => _text;
        set
        {
            if (SetProperty(ref _text, value))
            {
                _sendCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int MaxChatLength => SessionLimits.MaxChatLength;

    public ICommand SendCommand => _sendCommand;

    public ICommand PickCommand { get; }

    public ICommand CloseCommand { get; }
}
