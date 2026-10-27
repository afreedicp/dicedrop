namespace Ludo.Presentation.Wpf.Shell;

/// <summary>
/// One game per Windows session: with the app able to sit in the tray, a second copy would join the
/// lobby as a second player. A second launch instead asks the running copy to show itself.
/// </summary>
internal sealed class SingleInstance : IDisposable
{
    private const string SignalName = @"Local\DiceDrop.Activate.7E3C1B52";

    private readonly EventWaitHandle _signal;
    private RegisteredWaitHandle? _listener;

    private SingleInstance(EventWaitHandle signal) => _signal = signal;

    /// <summary>Returns null when another copy already runs; it has been asked to show itself unless <paramref name="quiet"/>.</summary>
    public static SingleInstance? TryClaim(bool quiet)
    {
        var signal = new EventWaitHandle(false, EventResetMode.AutoReset, SignalName, out var createdNew);
        if (createdNew)
        {
            return new SingleInstance(signal);
        }

        if (!quiet)
        {
            signal.Set();
        }

        signal.Dispose();
        return null;
    }

    /// <summary><paramref name="onActivate"/> runs on a thread-pool thread.</summary>
    public void ListenForActivation(Action onActivate) =>
        _listener = ThreadPool.RegisterWaitForSingleObject(_signal, (_, _) => onActivate(), null, Timeout.Infinite, executeOnlyOnce: false);

    public void Dispose()
    {
        _listener?.Unregister(null);
        _signal.Dispose();
    }
}
