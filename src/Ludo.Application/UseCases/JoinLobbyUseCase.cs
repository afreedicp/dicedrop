using Ludo.Application.Common;
using Ludo.Application.Session;

namespace Ludo.Application.UseCases;

public sealed class JoinLobbyUseCase
{
    private readonly SessionContext _context;
    private readonly SessionExecutor _executor;

    internal JoinLobbyUseCase(SessionContext context, SessionExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public Task ExecuteAsync(string playerName, string host, int port) =>
        _executor.RunAsync(() => JoinOnExecutorAsync(playerName, host, port));

    /// <summary>The join itself; callers must already be running on the session executor.</summary>
    internal async Task JoinOnExecutorAsync(string playerName, string host, int port)
    {
        _context.EnsureIdle();
        var name = SessionContext.ValidateName(playerName);
        if (string.IsNullOrWhiteSpace(host) || port is < 1 or > 65535)
        {
            throw new SessionException("Enter the host's address and port.");
        }

        _context.Begin(name);
        try
        {
            // Port 0: the OS picks a free port; peers learn it from the handshake.
            await _context.Network.StartAsync(_context.LocalId, 0).ConfigureAwait(false);
            _context.HostId = await _context.Network.ConnectAsync(host.Trim(), port).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _context.TeardownAsync().ConfigureAwait(false);
            throw new SessionException($"Could not reach {host}:{port}: {ex.Message}");
        }

        _context.Stage = SessionStage.Lobby;
        _context.AddLog($"Connected to {host}:{port}.");
        _context.Publish();
    }
}
