using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using Ludo.Application.Abstractions;
using Ludo.Infrastructure.Diagnostics;

namespace Ludo.Infrastructure.Discovery;

/// <summary>
/// Announces lobbies with a UDP broadcast beacon once a second and listens for others' beacons.
/// Beacons go to several fixed ports and browsers listen on every one they can bind, so discovery still
/// works when another program (or a Windows port reservation) holds one of them. Wi-Fi often drops
/// broadcasts, so once we hear a PC its beacons also go to it directly, which Wi-Fi delivers reliably:
/// one broadcast getting through either way is enough for both PCs to see each other.
/// </summary>
public sealed class UdpLobbyDiscovery(IDiagnosticLog? diagnostics = null) : ILobbyDiscovery
{
    private const string Source = "udp";
    /// <summary>IANA-unassigned ports, spread apart so one reserved block can't cover them all.</summary>
    public static readonly IReadOnlyList<int> DiscoveryPorts = [47778, 45778, 43778];
    private const string BeaconMagic = "LUDO-P2P/1";
    private static readonly TimeSpan BeaconInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan SummaryInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SummaryRepeat = TimeSpan.FromSeconds(60);

    /// <summary>How long a PC we heard keeps getting direct beacons after it falls silent.</summary>
    private static readonly TimeSpan DirectFor = TimeSpan.FromSeconds(30);

    private readonly IDiagnosticLog _log = diagnostics ?? NullDiagnosticLog.Instance;

    private readonly Lock _gate = new();
    private LobbyAdvertisement? _advertisement;
    private Timer? _beaconTimer;
    private UdpClient? _sender;
    private List<UdpClient>? _listeners;
    private CancellationTokenSource? _browsing;
    private Timer? _summaryTimer;

    /// <summary>Other PCs we heard a beacon from, and when; guarded by _gate.</summary>
    private readonly Dictionary<IPAddress, long> _direct = [];

    // Diagnostics only, guarded by _gate: who we heard since the last summary, and what was already logged.
    private readonly Dictionary<string, int> _heardFrom = [];
    private readonly HashSet<(Guid, string, int)> _announcedLobbies = [];
    private readonly HashSet<string> _failingTargets = [];
    private readonly HashSet<string> _strangers = [];
    private HashSet<string> _localAddresses = [];
    private int _sentRounds;
    private string? _lastSummary;
    private long _lastSummaryAt;

    public event EventHandler<DiscoveredLobby>? LobbyDiscovered;

    public void Advertise(LobbyAdvertisement advertisement)
    {
        lock (_gate)
        {
            if (_advertisement?.Port != advertisement.Port || _advertisement.LobbyId != advertisement.LobbyId)
            {
                _log.Write(Source, $"Announcing lobby \"{advertisement.HostName}\" (TCP port {advertisement.Port}) to UDP ports {string.Join(", ", DiscoveryPorts)} every second.");
            }

            _advertisement = advertisement;
            if (_beaconTimer is not null)
            {
                return;
            }

            _sentRounds = 0;
            _sender = new UdpClient(AddressFamily.InterNetwork) { EnableBroadcast = true };
            _beaconTimer = new Timer(_ => SendBeacon(), null, TimeSpan.Zero, BeaconInterval);
        }
    }

    public void StopAdvertising()
    {
        lock (_gate)
        {
            _beaconTimer?.Dispose();
            _beaconTimer = null;
            _sender?.Dispose();
            _sender = null;
            if (_advertisement is not null)
            {
                _log.Write(Source, "Stopped announcing our lobby.");
            }

            _advertisement = null;
        }
    }

    public void StartBrowsing()
    {
        lock (_gate)
        {
            if (_listeners is not null)
            {
                return;
            }

            // Reading the firewall can take a moment; never hold up the caller for it.
            _ = Task.Run(() => NetworkEnvironment.Describe(_log));
            _listeners = [];
            _browsing = new CancellationTokenSource();
            foreach (var port in DiscoveryPorts)
            {
                // Shared, so several copies of the game on one PC can all browse.
                var listener = new UdpClient(AddressFamily.InterNetwork);
                listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                try
                {
                    listener.Client.Bind(new IPEndPoint(IPAddress.Any, port));
                }
                catch (SocketException ex)
                {
                    // Taken or reserved: the other ports carry the same beacons. If none bind,
                    // joining by address still works.
                    _log.Write(Source, $"Can't listen on UDP port {port}: {ex.SocketErrorCode} ({ex.Message}).");
                    listener.Dispose();
                    continue;
                }

                _log.Write(Source, $"Listening for lobby beacons on UDP port {port}.");
                _listeners.Add(listener);
                _ = ReceiveLoopAsync(listener, _browsing.Token);
            }

            if (_listeners.Count == 0)
            {
                _log.Write(Source, "No discovery port could be opened, so this PC can't hear any lobby. Other PCs can still find ours.");
            }

            _heardFrom.Clear();
            _localAddresses = LocalAddresses();
            _summaryTimer = new Timer(_ => Summarize(), null, SummaryInterval, SummaryInterval);
        }
    }

    public void StopBrowsing()
    {
        lock (_gate)
        {
            _browsing?.Cancel();
            _browsing = null;
            _summaryTimer?.Dispose();
            _summaryTimer = null;
            foreach (var listener in _listeners ?? [])
            {
                listener.Dispose();
            }

            _listeners = null;
        }
    }

    public void Dispose()
    {
        StopAdvertising();
        StopBrowsing();
    }

    private void SendBeacon()
    {
        UdpClient? sender;
        byte[] payload;
        IPAddress[] direct;
        lock (_gate)
        {
            if (_advertisement is not { } ad || _sender is null)
            {
                return;
            }

            sender = _sender;
            payload = JsonSerializer.SerializeToUtf8Bytes(
                new Beacon(BeaconMagic, ad.LobbyId, ad.HostName, ad.Port, ad.Players, ad.MaxPlayers, ad.ProtocolVersion));

            var now = Environment.TickCount64;
            foreach (var (address, heardAt) in _direct.ToArray())
            {
                if (now - heardAt > DirectFor.TotalMilliseconds)
                {
                    _direct.Remove(address);
                    _log.Write(Source, $"{address} has been silent for {DirectFor.TotalSeconds:0} s; no longer sending beacons to it directly.");
                }
            }

            direct = [.. _direct.Keys];
        }

        var targets = BroadcastTargets().Concat(direct).Distinct().ToArray();
        foreach (var target in targets)
        {
            foreach (var port in DiscoveryPorts)
            {
                var endpoint = new IPEndPoint(target, port);
                try
                {
                    sender.Send(payload, payload.Length, endpoint);
                    lock (_gate)
                    {
                        if (_failingTargets.Remove(endpoint.ToString()))
                        {
                            _log.Write(Source, $"Beacons to {endpoint} go through again.");
                        }
                    }
                }
                catch (SocketException ex)
                {
                    // Interface went away. Logged once until it recovers, not every second.
                    lock (_gate)
                    {
                        if (_failingTargets.Add(endpoint.ToString()))
                        {
                            _log.Write(Source, $"Can't send beacon to {endpoint}: {ex.SocketErrorCode} ({ex.Message}).");
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Stopped mid-send.
                    return;
                }
            }
        }

        lock (_gate)
        {
            if (_sentRounds++ == 0)
            {
                _log.Write(Source, $"Beacons go to: {string.Join(", ", targets.Select(t => t.ToString()))}.");
            }
        }
    }

    /// <summary>
    /// Every few seconds, says who we heard: when that changes, else once a minute. Silence from every other
    /// PC is the usual sign of a firewall.
    /// </summary>
    private void Summarize()
    {
        lock (_gate)
        {
            if (_listeners is null)
            {
                return;
            }

            var window = $"{SummaryInterval.TotalSeconds:0} s";
            var senders = string.Join(", ", _heardFrom.Keys.Order(StringComparer.Ordinal));
            var now = Environment.TickCount64;
            if (senders != _lastSummary || now - _lastSummaryAt >= SummaryRepeat.TotalMilliseconds)
            {
                _lastSummary = senders;
                _lastSummaryAt = now;
                if (_heardFrom.Count == 0)
                {
                    _log.Write(Source, _advertisement is null
                        ? $"Heard no beacons in the last {window}."
                        : $"Heard no beacons in the last {window}, not even our own: something on this PC (firewall or security software) is blocking UDP for DiceDrop.");
                }
                else
                {
                    var detail = string.Join(", ", _heardFrom.Select(e => $"{e.Key} x{e.Value}"));
                    _log.Write(Source, _heardFrom.Keys.All(k => k.EndsWith(" (us)", StringComparison.Ordinal) || k.Contains("this PC", StringComparison.Ordinal))
                        ? $"Last {window}: heard only this PC ({detail}). No other PC is announcing a lobby; that is normal once everyone is in this one. " +
                          "If DiceDrop is open on a PC that isn't here, check Windows Firewall on BOTH PCs (allow DiceDrop on Private and Public networks), " +
                          "that both PCs are on the same subnet, and that the router's Wi-Fi client/AP isolation is off."
                        : $"Last {window}: heard {detail}.");
                }
            }

            _heardFrom.Clear();
        }
    }

    private static HashSet<string> LocalAddresses() =>
    [
        .. NetworkInterface.GetAllNetworkInterfaces()
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
            .Select(a => a.Address.ToString()),
    ];

    /// <summary>The limited broadcast plus each interface's subnet broadcast, so multi-NIC machines reach every LAN.</summary>
    private static IEnumerable<IPAddress> BroadcastTargets()
    {
        yield return IPAddress.Broadcast;
        foreach (var unicast in NetworkInterface.GetAllNetworkInterfaces()
                     .Where(n => n.OperationalStatus == OperationalStatus.Up)
                     .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                     .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && a.IPv4Mask is not null))
        {
            var address = unicast.Address.GetAddressBytes();
            var mask = unicast.IPv4Mask.GetAddressBytes();
            var broadcast = new byte[4];
            for (var i = 0; i < 4; i++)
            {
                broadcast[i] = (byte)(address[i] | ~mask[i]);
            }

            yield return new IPAddress(broadcast);
        }
    }

    private async Task ReceiveLoopAsync(UdpClient listener, CancellationToken cancellation)
    {
        while (!cancellation.IsCancellationRequested)
        {
            UdpReceiveResult datagram;
            try
            {
                datagram = await listener.ReceiveAsync(cancellation).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or SocketException)
            {
                return;
            }

            Beacon? beacon;
            try
            {
                beacon = JsonSerializer.Deserialize<Beacon>(datagram.Buffer);
            }
            catch (JsonException)
            {
                beacon = null;
            }

            var from = datagram.RemoteEndPoint.Address.ToString();
            if (beacon is not { Magic: BeaconMagic } || string.IsNullOrWhiteSpace(beacon.HostName) || beacon.Port is < 1 or > 65535)
            {
                lock (_gate)
                {
                    if (_strangers.Add(from))
                    {
                        _log.Write(Source, $"Ignored a packet from {from} that is not a DiceDrop beacon ({datagram.Buffer.Length} bytes).");
                    }
                }

                continue;
            }

            lock (_gate)
            {
                var ours = _advertisement?.LobbyId == beacon.LobbyId;
                var key = ours ? $"{from} (us)"
                    : _localAddresses.Contains(from) ? $"{from} ({beacon.HostName}, another copy on this PC)"
                    : $"{from} ({beacon.HostName})";
                _heardFrom[key] = _heardFrom.GetValueOrDefault(key) + 1;
                if (!ours && !_localAddresses.Contains(from))
                {
                    var address = datagram.RemoteEndPoint.Address;
                    if (!_direct.ContainsKey(address))
                    {
                        _log.Write(Source, $"Sending our beacons to {address} directly from now on, in case its broadcasts reach us but ours don't reach it.");
                    }

                    _direct[address] = Environment.TickCount64;
                }
                if (!ours && _announcedLobbies.Add((beacon.LobbyId, from, beacon.Port)))
                {
                    _log.Write(Source, $"Heard {beacon.HostName}'s lobby at {from}, TCP port {beacon.Port}, {beacon.Players}/{beacon.MaxPlayers} players, protocol v{beacon.Version}.");
                }
            }

            LobbyDiscovered?.Invoke(this, new DiscoveredLobby(
                beacon.LobbyId,
                beacon.HostName,
                datagram.RemoteEndPoint.Address.ToString(),
                beacon.Port,
                beacon.Players,
                beacon.MaxPlayers,
                beacon.Version));
        }
    }

    /// <param name="Version">Protocol version. Beacons from builds before it was added read as 0; the field is
    /// new, so those older builds still accept these beacons and just ignore it.</param>
    private sealed record Beacon(string Magic, Guid LobbyId, string HostName, int Port, int Players, int MaxPlayers, int Version);
}
