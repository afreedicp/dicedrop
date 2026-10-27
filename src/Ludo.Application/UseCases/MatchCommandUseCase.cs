using Ludo.Application.Session;
using Ludo.Application.Sync;

namespace Ludo.Application.UseCases;

/// <summary>
/// One local step in a lobby game's life: propose, answer, start, leave or stop. Each runs on the session
/// executor, then lets through any messages that were waiting for it and publishes the new state.
/// </summary>
public sealed class MatchCommandUseCase
{
    private readonly SessionContext _context;
    private readonly SessionExecutor _executor;
    private readonly PeerMessageRouter _router;
    private readonly Action _command;

    internal MatchCommandUseCase(SessionContext context, SessionExecutor executor, PeerMessageRouter router, Action command)
    {
        _context = context;
        _executor = executor;
        _router = router;
        _command = command;
    }

    public Task ExecuteAsync() => _executor.RunAsync(async () =>
    {
        _command();
        await _router.DrainDeferredAsync().ConfigureAwait(false);
        _context.Publish();
    });
}
