namespace Ludo.Application.Abstractions;

/// <summary>
/// Output port for connection diagnostics: what discovery and the network did and why, so a player can see
/// why two PCs don't find each other. Called from any thread; implementations must never throw or block.
/// </summary>
public interface IDiagnosticLog
{
    /// <param name="source">Short subsystem tag, such as "udp", "tcp" or "lobby".</param>
    void Write(string source, string message);
}

/// <summary>Discards everything; for hosts that show no diagnostics.</summary>
public sealed class NullDiagnosticLog : IDiagnosticLog
{
    public static NullDiagnosticLog Instance { get; } = new();

    private NullDiagnosticLog()
    {
    }

    public void Write(string source, string message)
    {
    }
}
