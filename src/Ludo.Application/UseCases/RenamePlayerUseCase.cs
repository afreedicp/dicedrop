using Ludo.Application.Session;
using Ludo.Application.Sync;

namespace Ludo.Application.UseCases;

/// <summary>Changes the local player's name. Outside a lobby it applies to the next lobby we find or open.</summary>
public sealed class RenamePlayerUseCase
{
    private readonly SessionContext _context;
    private readonly SessionExecutor _executor;
    private readonly LobbyProtocol _lobby;
    private readonly AutoLobbyUseCase _autoLobby;

    internal RenamePlayerUseCase(SessionContext context, SessionExecutor executor, LobbyProtocol lobby, AutoLobbyUseCase autoLobby)
    {
        _context = context;
        _executor = executor;
        _lobby = lobby;
        _autoLobby = autoLobby;
    }

    public Task ExecuteAsync(string newName) => _executor.RunAsync(() =>
    {
        if (_context.Stage == SessionStage.Idle)
        {
            _context.Rename(SessionContext.ValidateName(newName));
        }
        else
        {
            _lobby.RenameLocal(newName);
        }

        _autoLobby.Rename(_context.LocalName);
        _context.Publish();
    });
}
