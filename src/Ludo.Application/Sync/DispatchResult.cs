namespace Ludo.Application.Sync;

internal enum DispatchResult
{
    Handled,

    /// <summary>The message is valid but arrived ahead of a message it depends on; retry it later.</summary>
    Deferred,

    Ignored,
}
