using System.Threading.Channels;

namespace Ludo.Application.Session;

/// <summary>
/// Runs every state change (local commands and network input) one at a time, in arrival order.
/// This keeps session state single-threaded without locks.
/// </summary>
internal sealed class SessionExecutor : IAsyncDisposable
{
    private readonly Channel<Func<Task>> _queue = Channel.CreateUnbounded<Func<Task>>(
        new UnboundedChannelOptions { SingleReader = true });

    private readonly Action<Exception> _onUnhandled;
    private readonly Task _pump;

    public SessionExecutor(Action<Exception> onUnhandled)
    {
        _onUnhandled = onUnhandled;
        _pump = Task.Run(PumpAsync);
    }

    public Task RunAsync(Func<Task> work)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var queued = _queue.Writer.TryWrite(async () =>
        {
            try
            {
                await work();
                completion.SetResult();
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        });

        if (!queued)
        {
            completion.SetException(new ObjectDisposedException(nameof(SessionExecutor)));
        }

        return completion.Task;
    }

    public Task RunAsync(Action work) => RunAsync(() =>
    {
        work();
        return Task.CompletedTask;
    });

    /// <summary>Fire-and-forget; failures go to the unhandled-error callback.</summary>
    public void Post(Func<Task> work) => _queue.Writer.TryWrite(async () =>
    {
        try
        {
            await work();
        }
        catch (Exception ex)
        {
            _onUnhandled(ex);
        }
    });

    public void Post(Action work) => Post(() =>
    {
        work();
        return Task.CompletedTask;
    });

    public async ValueTask DisposeAsync()
    {
        _queue.Writer.TryComplete();
        await _pump.ConfigureAwait(false);
    }

    private async Task PumpAsync()
    {
        await foreach (var work in _queue.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            await work().ConfigureAwait(false);
        }
    }
}
