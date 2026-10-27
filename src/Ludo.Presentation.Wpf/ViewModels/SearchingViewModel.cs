namespace Ludo.Presentation.Wpf.ViewModels;

/// <summary>Shown while idle: the application is finding a lobby on the network, or about to open one.</summary>
public sealed class SearchingViewModel(string playerName) : ObservableObject
{
    private string _playerName = playerName;

    /// <summary>Follows renames made in the lobby, since the next lobby is joined under that name.</summary>
    public string PlayerName
    {
        get => _playerName;
        internal set => SetProperty(ref _playerName, value);
    }
}
