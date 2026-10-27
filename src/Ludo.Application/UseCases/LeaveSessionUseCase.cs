using Ludo.Application.Protocol;
using Ludo.Application.Session;

namespace Ludo.Application.UseCases;

public sealed class LeaveSessionUseCase
{
    private readonly SessionContext _context;
    private readonly SessionExecutor _executor;

    internal LeaveSessionUseCase(SessionContext context, SessionExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public Task ExecuteAsync() => _executor.RunAsync(async () =>
    {
        if (_context.Stage == SessionStage.Idle)
        {
            return;
        }

        _context.Network.Broadcast(new LeaveMessage());
        await _context.TeardownAsync().ConfigureAwait(false);
        _context.Publish();
    });
}
