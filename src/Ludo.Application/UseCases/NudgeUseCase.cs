using Ludo.Application.Session;
using Ludo.Application.Sync;

namespace Ludo.Application.UseCases;

/// <summary>Nudges one lobby member, at most once per <see cref="SessionLimits.NudgeCooldown"/>.</summary>
public sealed class NudgeUseCase
{
    private readonly SessionContext _context;
    private readonly SessionExecutor _executor;
    private readonly LobbyProtocol _lobby;

    internal NudgeUseCase(SessionContext context, SessionExecutor executor, LobbyProtocol lobby)
    {
        _context = context;
        _executor = executor;
        _lobby = lobby;
    }

    public Task ExecuteAsync(string peerKey) => _executor.RunAsync(() =>
    {
        _lobby.NudgeLocal(peerKey);
        _context.Publish();
    });
}
