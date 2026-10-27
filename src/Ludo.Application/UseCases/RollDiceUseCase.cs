using Ludo.Application.Session;
using Ludo.Application.Sync;

namespace Ludo.Application.UseCases;

public sealed class RollDiceUseCase
{
    private readonly SessionContext _context;
    private readonly SessionExecutor _executor;
    private readonly GameplayProtocol _gameplay;
    private readonly PeerMessageRouter _router;

    internal RollDiceUseCase(SessionContext context, SessionExecutor executor, GameplayProtocol gameplay, PeerMessageRouter router)
    {
        _context = context;
        _executor = executor;
        _gameplay = gameplay;
        _router = router;
    }

    public Task ExecuteAsync() => _executor.RunAsync(async () =>
    {
        _gameplay.RollLocal();
        await _router.DrainDeferredAsync().ConfigureAwait(false);
        _context.Publish();
    });
}
