using Ludo.Application.Session;
using Ludo.Application.Sync;

namespace Ludo.Application.UseCases;

/// <summary>Sends a private line to one lobby member. Like the group chat it lives in memory only.</summary>
public sealed class SendDirectChatUseCase
{
    private readonly SessionContext _context;
    private readonly SessionExecutor _executor;
    private readonly LobbyProtocol _lobby;

    internal SendDirectChatUseCase(SessionContext context, SessionExecutor executor, LobbyProtocol lobby)
    {
        _context = context;
        _executor = executor;
        _lobby = lobby;
    }

    public Task ExecuteAsync(string peerKey, string text) => _executor.RunAsync(() =>
    {
        _lobby.DirectChatLocal(peerKey, text);
        _context.Publish();
    });
}
