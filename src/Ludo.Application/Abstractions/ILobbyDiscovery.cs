namespace Ludo.Application.Abstractions;

/// <summary>Port for announcing and finding lobbies on the local network.</summary>
public interface ILobbyDiscovery : IDisposable
{
    event EventHandler<DiscoveredLobby>? LobbyDiscovered;

    /// <summary>Starts announcing, or replaces the announcement that is already running.</summary>
    void Advertise(LobbyAdvertisement advertisement);

    void StopAdvertising();

    void StartBrowsing();

    void StopBrowsing();
}

/// <param name="ProtocolVersion">Copies with a different version cannot play together, so they don't try.</param>
public sealed record LobbyAdvertisement(Guid LobbyId, string HostName, int Port, int Players, int MaxPlayers, int ProtocolVersion);

public sealed record DiscoveredLobby(Guid LobbyId, string HostName, string Address, int Port, int Players, int MaxPlayers, int ProtocolVersion);
