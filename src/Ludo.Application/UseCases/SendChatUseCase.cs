using Ludo.Application.Session;
using Ludo.Application.Sync;

namespace Ludo.Application.UseCases;

/// <summary>Sends a line to the lobby's group chat. Chat lives in memory only and is gone when the lobby ends.</summary>
public sealed class SendChatUseCase
{
    private readonly SessionContext _context;
    private readonly SessionExecutor _executor;
    private readonly LobbyProtocol _lobby;

    internal SendChatUseCase(SessionContext context, SessionExecutor executor, LobbyProtocol lobby)
    {
        _context = context;
        _executor = executor;
        _lobby = lobby;
    }

    public Task ExecuteAsync(string text) => _executor.RunAsync(() =>
    {
        _lobby.ChatLocal(text);
        _context.Publish();
    });
}
