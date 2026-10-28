using Ludo.Application.Session;
using Ludo.Application.Sync;

namespace Ludo.Application.UseCases;

/// <summary>Any lobby member: invite some or all of the lobby to a game from the catalog, which the caller then runs.</summary>
public sealed class ProposeGameUseCase
{
    private readonly SessionContext _context;
    private readonly SessionExecutor _executor;
    private readonly PeerMessageRouter _router;
    private readonly MatchProtocol _matches;

    internal ProposeGameUseCase(SessionContext context, SessionExecutor executor, PeerMessageRouter router, MatchProtocol matches)
    {
        _context = context;
        _executor = executor;
        _router = router;
        _matches = matches;
    }

    /// <param name="inviteeKeys">Member keys to invite; null or empty invites everyone.</param>
    public Task ExecuteAsync(string? gameId = null, IReadOnlyCollection<string>? inviteeKeys = null) => _executor.RunAsync(async () =>
    {
        _matches.ProposeLocal(gameId ?? GameCatalog.Ludo.Id, inviteeKeys);
        await _router.DrainDeferredAsync().ConfigureAwait(false);
        _context.Publish();
    });
}
