using System.Text;
using Ludo.Application.Abstractions;
using Ludo.Application.Common;
using Ludo.Application.Protocol;
using Ludo.Domain.Gameplay;
using Ludo.Domain.Players;

namespace Ludo.Application.Session;

/// <summary>
/// All mutable state of one local session plus the ports it talks to.
/// Only ever touched from the <see cref="SessionExecutor"/>, so it needs no locking.
/// </summary>
internal sealed class SessionContext(
    IPeerNetwork network,
    ILobbyDiscovery discovery,
    ICryptoProvider crypto,
    ISessionPresenter presenter)
{
    public const int MaxPlayers = 8;
    private const int MaxLogEntries = 200;
    private const int MaxChatEntries = 200;

    private readonly List<string> _log = [];
    private readonly List<ChatLineView> _chat = [];

    public IPeerNetwork Network { get; } = network;

    public ILobbyDiscovery Discovery { get; } = discovery;

    public ICryptoProvider Crypto { get; } = crypto;

    public ISessionPresenter Presenter { get; } = presenter;

    public SessionStage Stage { get; set; } = SessionStage.Idle;

    public PlayerId LocalId { get; private set; }

    public string LocalName { get; private set; } = string.Empty;

    public PlayerId? HostId { get; set; }

    public bool IsHost => Stage != SessionStage.Idle && HostId == LocalId;

    public Dictionary<PlayerId, PeerInfo> Peers { get; } = [];

    public List<RosterSeat> Roster { get; } = [];

    /// <summary>The lobby's current or most recent game; null until someone proposes one.</summary>
    public MatchState? Match { get; set; }

    public LudoGame? Game => Match?.Game;

    /// <summary>Matches that ended in this session; late messages for them are dropped, not parked.</summary>
    public HashSet<Guid> PastMatches { get; } = [];

    public List<IncomingMessage> Deferred { get; } = [];

    public IReadOnlyList<string> Log => _log;

    /// <summary>Group chat of the current lobby. Held in memory only and dropped with the session.</summary>
    public IReadOnlyList<ChatLineView> Chat => _chat;

    public static string ValidateName(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 or > Player.MaxNameLength)
        {
            throw new SessionException($"Enter a name of 1 to {Player.MaxNameLength} characters.");
        }

        return trimmed;
    }

    /// <summary>Trims a chat line and flattens line breaks; null if nothing sendable is left.</summary>
    public static string? NormalizeChat(string? text)
    {
        var flat = new string((text ?? string.Empty).Select(c => char.IsControl(c) ? ' ' : c).ToArray()).Trim();
        return flat.Length is 0 or > SessionLimits.MaxChatLength ? null : flat;
    }

    public void EnsureIdle()
    {
        if (Stage != SessionStage.Idle)
        {
            throw new SessionException("Leave the current lobby first.");
        }
    }

    public void Begin(string name)
    {
        Reset();
        LocalId = PlayerId.New();
        LocalName = name;
    }

    public void Rename(string name) => LocalName = name;

    public void Reset()
    {
        Stage = SessionStage.Idle;
        HostId = null;
        Match = null;
        PastMatches.Clear();
        Peers.Clear();
        Roster.Clear();
        Deferred.Clear();
        _log.Clear();
        _chat.Clear();
    }

    public async Task TeardownAsync()
    {
        Discovery.StopAdvertising();
        await Network.StopAsync().ConfigureAwait(false);
        Reset();
    }

    public void AddLog(string entry)
    {
        _log.Add(entry);
        if (_log.Count > MaxLogEntries)
        {
            _log.RemoveRange(0, _log.Count - MaxLogEntries);
        }
    }

    public void AddChat(string sender, string text, bool isLocal)
    {
        var line = new ChatLineView(sender, text, isLocal);
        _chat.Add(line);
        if (_chat.Count > MaxChatEntries)
        {
            _chat.RemoveRange(0, _chat.Count - MaxChatEntries);
        }

        Presenter.PresentChat(line);
    }

    public void Notify(string message) => Presenter.PresentNotice(message);

    public void Publish() => Presenter.PresentSession(SnapshotBuilder.Build(this));

    public string NameOf(PlayerId id) =>
        Roster.Find(r => r.Id == id)?.Name
        ?? Game?.FindPlayer(id)?.Name
        ?? (Peers.TryGetValue(id, out var peer) ? peer.Name : null)
        ?? id.ToString();

    /// <summary>Turns pending domain events into log lines and marks the match finished when the game ends.</summary>
    public void FlushGameEvents()
    {
        if (Match is not { Game: { } game } match)
        {
            return;
        }

        foreach (var domainEvent in game.DequeueEvents())
        {
            if (EventNarrator.Describe(domainEvent, game) is { } line)
            {
                AddLog(line);
            }

            if (domainEvent is GameEnded && match.Phase == MatchPhase.Playing)
            {
                match.Phase = MatchPhase.Finished;
                match.Chain = null;
                PastMatches.Add(match.Id);
            }
        }
    }

    /// <summary>
    /// Notes a change just applied to the board. The initiator forwards it to everyone not playing,
    /// which is how spectators follow the game without taking part in the dice protocol.
    /// </summary>
    public void RecordStep(MatchStep step)
    {
        if (Match is not { } match)
        {
            return;
        }

        match.History.Add(step);
        if (match.Initiator != LocalId)
        {
            return;
        }

        var message = new MatchStepMessage(match.Id, match.History.Count - 1, step);
        foreach (var watcher in Peers.Values.Where(p => p.IsGreeted && !match.IsActivePlayer(p.Id)))
        {
            Network.Send(watcher.Id, message);
        }
    }

    /// <summary>Lobby members, other than the initiator, who said yes to the open invite and are connected to us.</summary>
    public List<RosterSeat> AcceptedPlayers() =>
        Match is { Phase: MatchPhase.Inviting } match
            ? Roster
                .Where(r => r.Id != match.Initiator
                    && match.Replies.TryGetValue(r.Id, out var reply) && reply.Accepted
                    && Peers.TryGetValue(r.Id, out var peer) && peer.IsGreeted)
                .ToList()
            : [];

    /// <summary>Sends to every other player still in the current match.</summary>
    public void SendToPlayers(PeerMessage message)
    {
        if (Match is not { } match)
        {
            return;
        }

        foreach (var peer in Peers.Keys.Where(id => match.IsActivePlayer(id)))
        {
            Network.Send(peer, message);
        }
    }

    /// <summary>Closes the current invite or game for good; the board stays visible until the next one.</summary>
    public void EndMatch(string reason)
    {
        if (Match is not { IsActive: true } match)
        {
            return;
        }

        match.Phase = MatchPhase.Stopped;
        match.EndReason = reason;
        match.IsPlaying = false;
        match.Chain = null;
        match.Dice.Reset();
        PastMatches.Add(match.Id);
        AddLog(reason);
    }

    /// <summary>Short fingerprint of the replicated game state, compared between peers to spot desyncs.</summary>
    public string ComputeDigest()
    {
        if (Game is not { } game)
        {
            return string.Empty;
        }

        var text = new StringBuilder()
            .Append(game.RollCount).Append('|')
            .Append(game.CurrentSeat).Append('|')
            .Append((int)game.Phase).Append('|')
            .Append(game.ConsecutiveSixes);
        foreach (var player in game.Players)
        {
            text.Append('|').Append((int)player.Status);
            foreach (var token in game.TokensOf(player.Seat))
            {
                text.Append(',').Append(token.Progress);
            }
        }

        return Convert.ToHexString(Crypto.Hash(Encoding.UTF8.GetBytes(text.ToString())))[..16];
    }
}
