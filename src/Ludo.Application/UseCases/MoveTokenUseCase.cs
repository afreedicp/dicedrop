using Ludo.Application.Session;
using Ludo.Application.Sync;

namespace Ludo.Application.UseCases;

public sealed class MoveTokenUseCase
{
    private readonly SessionContext _context;
    private readonly SessionExecutor _executor;
    private readonly GameplayProtocol _gameplay;
    private readonly PeerMessageRouter _router;

    internal MoveTokenUseCase(SessionContext context, SessionExecutor executor, GameplayProtocol gameplay, PeerMessageRouter router)
    {
        _context = context;
        _executor = executor;
        _gameplay = gameplay;
        _router = router;
    }

    public Task ExecuteAsync(int tokenIndex) => _executor.RunAsync(async () =>
    {
        _gameplay.MoveLocal(tokenIndex);
        await _router.DrainDeferredAsync().ConfigureAwait(false);
        _context.Publish();
    });
}
