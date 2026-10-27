using Ludo.Application.Common;
using Ludo.Application.Protocol;
using Ludo.Application.Session;
using Ludo.Application.Sync;

namespace Ludo.Application.UseCases;

public sealed class HostLobbyUseCase
{
    private readonly SessionContext _context;
    private readonly SessionExecutor _executor;
    private readonly LobbyProtocol _lobby;

    internal HostLobbyUseCase(SessionContext context, SessionExecutor executor, LobbyProtocol lobby)
    {
        _context = context;
        _executor = executor;
        _lobby = lobby;
    }

    /// <summary>
    /// Opens a lobby on <paramref name="port"/>, or on a free port the OS picks if something else holds it.
    /// LAN discovery announces whichever port we got, so joiners never need to know it.
    /// </summary>
    public Task ExecuteAsync(string playerName, int port = ProtocolInfo.PreferredPort) =>
        _executor.RunAsync(() => HostOnExecutorAsync(playerName, port));

    /// <summary>The hosting itself; callers must already be running on the session executor.</summary>
    internal async Task HostOnExecutorAsync(string playerName, int port)
    {
        _context.EnsureIdle();
        var name = SessionContext.ValidateName(playerName);
        if (port is < 1 or > 65535)
        {
            throw new SessionException("Choose a port between 1 and 65535.");
        }

        _context.Begin(name);
        string? fallbackReason = null;
        try
        {
            await _context.Network.StartAsync(_context.LocalId, port).ConfigureAwait(false);
        }
        catch (Exception preferred)
        {
            fallbackReason = preferred.Message;
            try
            {
                await _context.Network.StartAsync(_context.LocalId, 0).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _context.TeardownAsync().ConfigureAwait(false);
                throw new SessionException($"Could not open a network port: {ex.Message}");
            }
        }

        _context.HostId = _context.LocalId;
        _context.Stage = SessionStage.Lobby;
        _context.Roster.Add(new RosterSeat(_context.LocalId, name));
        if (fallbackReason is not null)
        {
            _context.AddLog($"Port {port} is in use by another program ({fallbackReason}).");
        }

        _context.AddLog($"Lobby open on port {_context.Network.ListenPort}.");
        _lobby.BroadcastRoster();
        _context.Publish();
    }
}
