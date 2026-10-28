using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using Ludo.Application;
using Ludo.Application.Session;

namespace Ludo.Presentation.Wpf.ViewModels;

public sealed record LobbyMemberItem(string Name, string Tags, bool IsReady, bool InGame)
{
    public string Initials => ChatMessageItem.InitialsOf(Name);

    public Brush AvatarColor => ChatMessageItem.AvatarColorOf(Name);
}

/// <param name="Answer">"Started it", "In", "Out" or "…".</param>
public sealed record InviteeItem(string Name, string Answer, bool IsIn);

public sealed class LobbyViewModel : ObservableObject
{
    private readonly AsyncCommand _saveNameCommand;
    private readonly AsyncCommand _sendChatCommand;
    private readonly AsyncCommand _proposeCommand;
    private readonly AsyncCommand _beginCommand;
    private IReadOnlyList<LobbyMemberItem> _members = [];
    private IReadOnlyList<InviteeItem> _invitees = [];
    private string _status = string.Empty;
    private string _localName = string.Empty;
    private bool _isRenaming;
    private string _nameDraft = string.Empty;
    private string _chatDraft = string.Empty;
    private bool _canPropose;
    private bool _canBegin;
    private string _gameTitle = string.Empty;
    private string _gameDetail = string.Empty;
    private string _beginLabel = "Begin game";
    private string _watchLabel = "Watch";
    private bool _isInvitedNow;
    private bool _showPropose;
    private bool _showBegin;
    private bool _showCancel;
    private bool _showAccept;
    private bool _showDecline;
    private bool _showWatch;
    private bool _showStop;

    internal LobbyViewModel(LudoApplication app, Action<Exception> onError, Action viewGame)
    {
        _saveNameCommand = new AsyncCommand(
            async _ =>
            {
                await app.RenamePlayer.ExecuteAsync(NameDraft);
                IsRenaming = false;
            },
            onError,
            _ => !string.IsNullOrWhiteSpace(NameDraft));
        _sendChatCommand = new AsyncCommand(
            async _ =>
            {
                await app.SendChat.ExecuteAsync(ChatDraft);
                ChatDraft = string.Empty;
            },
            onError,
            _ => !string.IsNullOrWhiteSpace(ChatDraft));
        _proposeCommand = new AsyncCommand(_ => app.ProposeGame.ExecuteAsync(), onError, _ => CanPropose);
        _beginCommand = new AsyncCommand(_ => app.StartGame.ExecuteAsync(), onError, _ => CanBegin);

        RenameCommand = new AsyncCommand(_ => { NameDraft = LocalName; IsRenaming = true; return Task.CompletedTask; }, onError);
        CancelRenameCommand = new AsyncCommand(_ => { IsRenaming = false; return Task.CompletedTask; }, onError);
        AcceptCommand = new AsyncCommand(_ => app.AcceptInvite.ExecuteAsync(), onError);
        DeclineCommand = new AsyncCommand(_ => app.DeclineInvite.ExecuteAsync(), onError);
        StopCommand = new AsyncCommand(_ => app.StopGame.ExecuteAsync(), onError);
        WatchCommand = new AsyncCommand(_ => { viewGame(); return Task.CompletedTask; }, onError);
    }

    public IReadOnlyList<LobbyMemberItem> Members
    {
        get => _members;
        private set
        {
            if (SetProperty(ref _members, value))
            {
                OnPropertyChanged(nameof(MemberCount));
            }
        }
    }

    public int MemberCount => Members.Count;

    public ObservableCollection<ChatMessageItem> Chat { get; } = [];

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public string LocalName
    {
        get => _localName;
        private set => SetProperty(ref _localName, value);
    }

    public bool IsRenaming
    {
        get => _isRenaming;
        private set => SetProperty(ref _isRenaming, value);
    }

    public string NameDraft
    {
        get => _nameDraft;
        set
        {
            if (SetProperty(ref _nameDraft, value))
            {
                _saveNameCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string ChatDraft
    {
        get => _chatDraft;
        set
        {
            if (SetProperty(ref _chatDraft, value))
            {
                _sendChatCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int MaxNameLength => SessionLimits.MaxNameLength;

    public int MaxChatLength => SessionLimits.MaxChatLength;

    public bool CanPropose
    {
        get => _canPropose;
        private set
        {
            if (SetProperty(ref _canPropose, value))
            {
                _proposeCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanBegin
    {
        get => _canBegin;
        private set
        {
            if (SetProperty(ref _canBegin, value))
            {
                _beginCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string GameTitle
    {
        get => _gameTitle;
        private set => SetProperty(ref _gameTitle, value);
    }

    public string GameDetail
    {
        get => _gameDetail;
        private set => SetProperty(ref _gameDetail, value);
    }

    public IReadOnlyList<InviteeItem> Invitees
    {
        get => _invitees;
        private set => SetProperty(ref _invitees, value);
    }

    /// <summary>An invite is waiting for our answer; the card is highlighted.</summary>
    public bool IsInvitedNow
    {
        get => _isInvitedNow;
        private set => SetProperty(ref _isInvitedNow, value);
    }

    public string BeginLabel
    {
        get => _beginLabel;
        private set => SetProperty(ref _beginLabel, value);
    }

    public string WatchLabel
    {
        get => _watchLabel;
        private set => SetProperty(ref _watchLabel, value);
    }

    public bool ShowPropose
    {
        get => _showPropose;
        private set => SetProperty(ref _showPropose, value);
    }

    public bool ShowBegin
    {
        get => _showBegin;
        private set => SetProperty(ref _showBegin, value);
    }

    public bool ShowCancel
    {
        get => _showCancel;
        private set => SetProperty(ref _showCancel, value);
    }

    public bool ShowAccept
    {
        get => _showAccept;
        private set => SetProperty(ref _showAccept, value);
    }

    public bool ShowDecline
    {
        get => _showDecline;
        private set => SetProperty(ref _showDecline, value);
    }

    public bool ShowWatch
    {
        get => _showWatch;
        private set => SetProperty(ref _showWatch, value);
    }

    public bool ShowStop
    {
        get => _showStop;
        private set => SetProperty(ref _showStop, value);
    }

    public ICommand RenameCommand { get; }

    public ICommand SaveNameCommand => _saveNameCommand;

    public ICommand CancelRenameCommand { get; }

    public ICommand SendChatCommand => _sendChatCommand;

    public ICommand ProposeCommand => _proposeCommand;

    public ICommand BeginCommand => _beginCommand;

    public ICommand AcceptCommand { get; }

    public ICommand DeclineCommand { get; }

    /// <summary>Withdraws our invite or stops our game.</summary>
    public ICommand StopCommand { get; }

    public ICommand WatchCommand { get; }

    internal void Update(SessionSnapshot snapshot)
    {
        Members = snapshot.Members
            .Select(m => new LobbyMemberItem(
                m.Name,
                string.Join("  ·  ", new[] { m.IsHost ? "host" : null, m.IsLocal ? "you" : null }.OfType<string>()),
                m.IsReady,
                m.InGame))
            .ToArray();
        LocalName = snapshot.LocalName;
        CanPropose = snapshot.CanProposeGame;
        SyncChat(snapshot.Chat);

        var connecting = snapshot.Members.Count(m => !m.IsReady);
        Status = snapshot.Members.Count < 2 ? "Waiting for players on your network…"
            : connecting > 0 ? $"Connecting players to each other ({connecting} pending)…"
            : $"{snapshot.Members.Count} players in the lobby. Anyone can start a game.";

        UpdateGameCard(snapshot);
    }

    private void UpdateGameCard(SessionSnapshot snapshot)
    {
        var match = snapshot.Match;
        var inviting = match is { Phase: MatchPhaseView.Inviting };
        var running = match is { Phase: MatchPhaseView.Playing };
        var mine = match?.IsInitiator == true;
        var answer = match?.LocalAnswer ?? InviteAnswer.Pending;

        ShowPropose = !inviting && !running;
        ShowBegin = inviting && mine;
        ShowCancel = inviting && mine;
        ShowAccept = inviting && !mine && answer != InviteAnswer.Accepted;
        ShowDecline = inviting && !mine && answer == InviteAnswer.Accepted;
        ShowStop = running && mine;
        ShowWatch = match?.Game is not null;
        WatchLabel = running ? (match!.IsPlaying ? "Return to game" : "Watch") : "View last game";
        IsInvitedNow = inviting && !mine && answer == InviteAnswer.Pending;
        CanBegin = match?.CanStart == true;

        Invitees = inviting
            ? match!.Invitees
                .Select(i => new InviteeItem(
                    i.IsLocal ? $"{i.Name} (you)" : i.Name,
                    i.IsInitiator ? "Started it" : i.Answer switch { InviteAnswer.Accepted => "In", InviteAnswer.Declined => "Out", _ => "…" },
                    i.Answer == InviteAnswer.Accepted))
                .ToArray()
            : [];
        var players = match?.Invitees.Count(i => i.Answer == InviteAnswer.Accepted) ?? 0;
        BeginLabel = $"Begin game ({players} players)";

        (GameTitle, GameDetail) = match switch
        {
            { Phase: MatchPhaseView.Inviting, IsInitiator: true } => (
                "Waiting for players",
                players > 1 ? "Begin whenever everyone who wants to play has said yes." : "Nobody has said yes yet."),
            { Phase: MatchPhaseView.Inviting } when answer == InviteAnswer.Accepted => (
                "You're in!",
                $"Waiting for {match.InitiatorName} to begin the game…"),
            { Phase: MatchPhaseView.Inviting } when answer == InviteAnswer.Declined => (
                $"{match.InitiatorName} is setting up a game",
                "You said not now. You can still join until it begins."),
            { Phase: MatchPhaseView.Inviting } => (
                $"{match.InitiatorName} wants to play Ludo",
                "Join in, or skip this one and watch."),
            { Phase: MatchPhaseView.Playing } => (
                match.IsInitiator ? "Your game is on" : $"{match.InitiatorName}'s game is on",
                $"{string.Join(", ", match.Game?.Seats.Where(s => s.Standing != SeatStanding.Left).Select(s => s.Name) ?? [])} playing."),
            _ when !snapshot.CanProposeGame => (
                "Play Ludo",
                "Waiting for someone else to join the lobby…"),
            _ => (
                "Play Ludo",
                "Invite everyone in the lobby. Whoever accepts plays; the rest can watch."),
        };
    }

    /// <summary>
    /// Keeps the chat in place so it holds its scroll position and only new lines animate in: drops lines trimmed
    /// from the front of the history, appends new ones, and rebuilds only when the history was replaced.
    /// </summary>
    private void SyncChat(IReadOnlyList<ChatLineView> lines)
    {
        var trimmed = Enumerable.Range(0, Chat.Count + 1)
            .First(skip => skip == Chat.Count
                || (Chat.Count - skip <= lines.Count && Chat.Skip(skip).Select((item, i) => item.Line == lines[i]).All(same => same)));
        for (var i = 0; i < trimmed; i++)
        {
            Chat.RemoveAt(0);
        }

        if (Chat.Count > 0 && !Chat[0].StartsGroup)
        {
            Chat[0] = Chat[0] with { StartsGroup = true };
        }

        foreach (var line in lines.Skip(Chat.Count))
        {
            var startsGroup = Chat.Count == 0 || Chat[^1].Sender != line.Sender || Chat[^1].IsLocal != line.IsLocal;
            Chat.Add(new ChatMessageItem(line, startsGroup, DateTime.Now));
        }
    }
}
