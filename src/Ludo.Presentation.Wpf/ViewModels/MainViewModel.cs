using System.Windows.Input;
using Ludo.Application;
using Ludo.Application.Common;
using Ludo.Application.Session;
using Ludo.Presentation.Wpf.Presenters;

namespace Ludo.Presentation.Wpf.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private object _currentScreen;
    private string? _notice;
    private SessionSnapshot _snapshot = SessionSnapshot.Empty;

    // Which board is on screen is a view concern: the session only says which games exist.
    private Guid? _viewing;
    private Guid? _autoOpened;

    internal MainViewModel(LudoApplication app, WpfSessionPresenter presenter, WpfDiagnosticLog diagnostics)
    {
        Diagnostics = new DiagnosticsViewModel(diagnostics, ShowError);

        // No setup screen: play under the Windows account name.
        Searching = new SearchingViewModel(app.AutoLobby.Start(Environment.UserName));
        Lobby = new LobbyViewModel(app, ShowError, ViewGame);
        Game = new GameViewModel(app, ShowError, Lobby, BackToLobby);
        DismissNoticeCommand = new AsyncCommand(_ => { Notice = null; return Task.CompletedTask; }, ShowError);

        presenter.SessionChanged += Apply;
        presenter.NoticeRaised += message => Notice = message;

        _currentScreen = Searching;
    }

    public SearchingViewModel Searching { get; }

    public DiagnosticsViewModel Diagnostics { get; }

    public LobbyViewModel Lobby { get; }

    public GameViewModel Game { get; }

    public object CurrentScreen
    {
        get => _currentScreen;
        private set => SetProperty(ref _currentScreen, value);
    }

    public string? Notice
    {
        get => _notice;
        private set => SetProperty(ref _notice, value);
    }

    public ICommand DismissNoticeCommand { get; }

    private void Apply(SessionSnapshot snapshot)
    {
        _snapshot = snapshot;
        Refresh();
    }

    private void Refresh()
    {
        var snapshot = _snapshot;
        if (snapshot.Stage == SessionStage.Idle)
        {
            _viewing = null;
            Game.Clear();
            CurrentScreen = Searching;
            return;
        }

        // Players are taken to the board when their game begins, once; after that they come and go freely.
        var match = snapshot.Match;
        if (match is { IsPlaying: true, Phase: MatchPhaseView.Playing } && _autoOpened != match.Id)
        {
            _autoOpened = match.Id;
            _viewing = match.Id;
        }

        if (match?.Game is null || match.Id != _viewing)
        {
            _viewing = null;
        }

        Lobby.Update(snapshot);
        Searching.PlayerName = snapshot.LocalName;
        if (_viewing is null)
        {
            Game.Clear();
            CurrentScreen = Lobby;
            return;
        }

        Game.Update(snapshot);
        CurrentScreen = Game;
    }

    private void ViewGame()
    {
        if (_snapshot.Match is { Game: not null } match)
        {
            _viewing = match.Id;
            Refresh();
        }
    }

    private void BackToLobby()
    {
        _viewing = null;
        Refresh();
    }

    private void ShowError(Exception error) =>
        Notice = error is SessionException ? error.Message : $"Something went wrong: {error.Message}";
}
