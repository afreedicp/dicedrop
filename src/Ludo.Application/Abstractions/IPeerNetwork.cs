using Ludo.Application.Protocol;
using Ludo.Domain.Players;

namespace Ludo.Application.Abstractions;

/// <summary>
/// Transport port for a full mesh of peers. Implementations must deliver messages from one peer in the
/// order they were sent, and raise <see cref="PeerConnected"/> before any message from that peer.
/// Send and Broadcast only enqueue; they never block on the network.
/// </summary>
public interface IPeerNetwork
{
    event EventHandler<PeerConnection>? PeerConnected;

    event EventHandler<PlayerId>? PeerDisconnected;

    event EventHandler<IncomingMessage>? MessageReceived;

    int ListenPort { get; }

    Task StartAsync(PlayerId localId, int port, CancellationToken cancellationToken = default);

    /// <summary>Opens a link to a listening peer and returns its identity once the handshake succeeds.</summary>
    Task<PlayerId> ConnectAsync(string host, int port, CancellationToken cancellationToken = default);

    void Send(PlayerId peer, PeerMessage message);

    void Broadcast(PeerMessage message);

    void Disconnect(PlayerId peer);

    Task StopAsync();

    IReadOnlyList<string> GetLocalAddresses();
}

/// <param name="Host">Address the peer was reached on.</param>
/// <param name="ListenPort">Port the peer accepts connections on, so others can mesh with it.</param>
public sealed record PeerConnection(PlayerId Peer, string Host, int ListenPort);

public sealed record IncomingMessage(PlayerId From, PeerMessage Message);
