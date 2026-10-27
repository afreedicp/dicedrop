using Ludo.Application.Common;
using Ludo.Application.FairDice;
using Ludo.Application.Protocol;
using Ludo.Application.Session;
using Ludo.Domain.Common;
using Ludo.Domain.Gameplay;
using Ludo.Domain.Players;

namespace Ludo.Application.Sync;

/// <summary>
/// Games inside the lobby. Any member can ask who wants to play; everyone is invited, and those who say
/// yes commit to a fresh dice chain. The initiator starts the game with them, can stop it at any time,
/// and keeps everyone who is not playing (spectators, and players who left) up to date with its record
/// of the match. One match runs at a time: if two invites cross, the one with the lower id wins everywhere.
/// </summary>
internal sealed class MatchProtocol(SessionContext context, GameplayProtocol gameplay)
{
    public void ProposeLocal()
    {
        RequireLobby();
        if (context.Match is { IsActive: true })
        {
            throw new SessionException("A game is already being set up or played in this lobby.");
        }

        if (context.Roster.Count < 2)
        {
            throw new SessionException("Wait for someone else to join the lobby first.");
        }

        var match = new MatchState(Guid.NewGuid(), context.LocalId) { Chain = HashChain.Create(context.Crypto) };
        match.Replies[context.LocalId] = new MatchReply(true, match.Chain.Anchor);
        context.Match = match;
        context.Network.Broadcast(new MatchProposalMessage(match.Id, match.Chain.Anchor));
        context.AddLog("You asked who wants to play.");
    }

    public void AnswerLocal(bool accept)
    {
        if (context.Match is not { Phase: MatchPhase.Inviting } match)
        {
            throw new SessionException("There is no open invite.");
        }

        if (match.Initiator == context.LocalId)
        {
            throw new SessionException("You started this invite.");
        }

        match.Chain = accept ? HashChain.Create(context.Crypto) : null;
        var anchor = match.Chain?.Anchor;
        match.Replies[context.LocalId] = new MatchReply(accept, anchor);
        context.Network.Broadcast(new MatchReplyMessage(match.Id, accept, anchor));
    }

    public void StartLocal()
    {
        if (context.Match is not { Phase: MatchPhase.Inviting } match || match.Initiator != context.LocalId)
        {
            throw new SessionException("Only whoever sent the invite can start the game.");
        }

        var players = context.AcceptedPlayers();
        if (players.Count == 0)
        {
            throw new SessionException("Nobody has accepted yet.");
        }

        if (players.Any(p => !p.Ready))
        {
            throw new SessionException("Some players are still connecting to each other.");
        }

        var seats = players
            .Prepend(context.Roster.Find(r => r.Id == context.LocalId) ?? new RosterSeat(context.LocalId, context.LocalName))
            .Take(SessionContext.MaxPlayers)
            .Select(r => new SeatAssignment(r.Id, r.Name, match.Replies[r.Id].Anchor!))
            .ToArray();
        var start = new GameStartMessage(match.Id, GameRules.Classic, seats);
        context.Network.Broadcast(start);
        ApplyStart(match, start);
    }

    public void LeaveLocal()
    {
        if (context.Match is not { Phase: MatchPhase.Playing, IsPlaying: true, Game: { } game } match)
        {
            throw new SessionException("You are not playing in a game.");
        }

        if (match.Initiator == context.LocalId)
        {
            throw new SessionException("You started this game, so stop it instead.");
        }

        context.Network.Broadcast(new MatchLeaveMessage(match.Id));
        gameplay.DropPlayer(game, context.LocalId);
        match.IsPlaying = false;
        match.Chain = null;
        context.AddLog("You left the game. You can keep watching it.");
    }

    public void StopLocal()
    {
        if (context.Match is not { IsActive: true } match || match.Initiator != context.LocalId)
        {
            throw new SessionException("Only whoever started the game can stop it.");
        }

        context.Network.Broadcast(new MatchEndMessage(match.Id));
        context.EndMatch(match.Phase == MatchPhase.Inviting ? "You withdrew the invite." : "You stopped the game.");
    }

    public DispatchResult OnProposal(PlayerId from, MatchProposalMessage proposal)
    {
        if (!IsGreeted(from) || proposal.ChainAnchor is not { Length: HashChain.ValueLength }
            || context.PastMatches.Contains(proposal.MatchId))
        {
            return DispatchResult.Ignored;
        }

        if (context.Match is { IsActive: true } current)
        {
            if (current.Id == proposal.MatchId)
            {
                return DispatchResult.Ignored;
            }

            // A running game beats any invite, and of two crossing invites the lower id wins on every peer.
            if (current.Phase == MatchPhase.Playing || current.Id.CompareTo(proposal.MatchId) < 0)
            {
                context.PastMatches.Add(proposal.MatchId);
                if (current.Phase == MatchPhase.Playing && current.Initiator == context.LocalId)
                {
                    SendReplay(from, current);
                }

                return DispatchResult.Ignored;
            }

            Supersede(current, $"{context.NameOf(from)} asked first, so your invite was withdrawn.");
        }

        var match = new MatchState(proposal.MatchId, from);
        match.Replies[from] = new MatchReply(true, proposal.ChainAnchor);
        context.Match = match;
        var name = context.NameOf(from);
        context.AddLog($"{name} wants to play.");
        context.Presenter.PresentInvite(new InviteView(match.Id, name));
        return DispatchResult.Handled;
    }

    public DispatchResult OnReply(PlayerId from, MatchReplyMessage reply)
    {
        if (Find(reply.MatchId) is not { } match)
        {
            return Unknown(reply.MatchId);
        }

        if (match.Phase != MatchPhase.Inviting || from == match.Initiator || !IsGreeted(from)
            || (reply.Accepted && reply.ChainAnchor is not { Length: HashChain.ValueLength }))
        {
            return DispatchResult.Ignored;
        }

        match.Replies[from] = new MatchReply(reply.Accepted, reply.Accepted ? reply.ChainAnchor : null);
        return DispatchResult.Handled;
    }

    public DispatchResult OnGameStart(PlayerId from, GameStartMessage start)
    {
        if (Find(start.MatchId) is not { } match)
        {
            return Unknown(start.MatchId);
        }

        if (from != match.Initiator || match.Phase != MatchPhase.Inviting)
        {
            return DispatchResult.Ignored;
        }

        // Each player's reply travels on its own link, so it may still be on the way to us.
        if (start.Seats.Any(s => s.Id == context.LocalId)
            && start.Seats.Any(s => s.Id != context.LocalId && !match.Replies.ContainsKey(s.Id) && context.Peers.ContainsKey(s.Id)))
        {
            return DispatchResult.Deferred;
        }

        ApplyStart(match, start);
        return DispatchResult.Handled;
    }

    public DispatchResult OnLeave(PlayerId from, MatchLeaveMessage leave)
    {
        if (Find(leave.MatchId) is not { } match)
        {
            return Unknown(leave.MatchId);
        }

        if (match.Phase != MatchPhase.Playing || match.Game is not { } game)
        {
            return DispatchResult.Ignored;
        }

        if (match.IsPlaying)
        {
            gameplay.DropPlayer(game, from);
        }

        if (match.Initiator == context.LocalId)
        {
            // From now on the leaver follows the game as a spectator.
            SendReplay(from, match);
        }

        return DispatchResult.Handled;
    }

    public DispatchResult OnEnd(PlayerId from, MatchEndMessage end)
    {
        if (Find(end.MatchId) is not { IsActive: true } match || from != match.Initiator)
        {
            return DispatchResult.Ignored;
        }

        var name = context.NameOf(from);
        context.EndMatch(match.Phase == MatchPhase.Inviting ? $"{name} withdrew the invite." : $"{name} stopped the game.");
        return DispatchResult.Handled;
    }

    public DispatchResult OnReplay(PlayerId from, MatchReplayMessage replay)
    {
        if (context.PastMatches.Contains(replay.MatchId))
        {
            return DispatchResult.Ignored;
        }

        var match = Find(replay.MatchId);
        if (match is null)
        {
            // A game that was already running when we arrived, or one that beat our own invite.
            if (context.Match is { IsActive: true } current)
            {
                if (current.Phase == MatchPhase.Playing)
                {
                    return DispatchResult.Ignored;
                }

                Supersede(current, "A game is already running, so your invite was withdrawn.");
            }

            match = new MatchState(replay.MatchId, from);
        }
        else if (from != match.Initiator || match.IsPlaying || !match.IsActive)
        {
            return DispatchResult.Ignored;
        }

        LudoGame game;
        try
        {
            game = LudoGame.Start(match.Id, replay.Seats.Select(s => (s.Id, s.Name)).ToArray(), replay.Rules);
            foreach (var step in replay.Steps)
            {
                Apply(game, step);
            }
        }
        catch (DomainException ex)
        {
            context.AddLog($"Could not follow the game: {ex.Message}");
            return DispatchResult.Ignored;
        }

        // History is replayed silently; only what happens from now on is narrated.
        game.DequeueEvents();
        if (context.Match != match)
        {
            context.Match = match;
            context.AddLog($"{context.NameOf(from)}'s game is in progress. You can watch it.");
        }

        match.Rules = replay.Rules;
        match.Seats = replay.Seats;
        match.Game = game;
        match.IsPlaying = false;
        match.AwaitingReplay = false;
        match.Dice.Reset();
        match.History.Clear();
        match.History.AddRange(replay.Steps);
        match.Phase = MatchPhase.Playing;
        if (game.Phase == TurnPhase.GameOver)
        {
            match.Phase = MatchPhase.Finished;
            context.PastMatches.Add(match.Id);
        }

        return DispatchResult.Handled;
    }

    public DispatchResult OnStep(PlayerId from, MatchStepMessage message)
    {
        if (Find(message.MatchId) is not { Phase: MatchPhase.Playing, IsPlaying: false, Game: { } game } match
            || from != match.Initiator || message.Index < match.History.Count)
        {
            return DispatchResult.Ignored;
        }

        if (message.Index > match.History.Count)
        {
            RequestReplay(match);
            return DispatchResult.Ignored;
        }

        try
        {
            Apply(game, message.Step);
        }
        catch (DomainException)
        {
            RequestReplay(match);
            return DispatchResult.Ignored;
        }

        match.History.Add(message.Step);
        context.FlushGameEvents();
        return DispatchResult.Handled;
    }

    public DispatchResult OnSyncRequest(PlayerId from, MatchSyncRequestMessage request)
    {
        if (Find(request.MatchId) is { Phase: MatchPhase.Playing or MatchPhase.Finished } match
            && match.Initiator == context.LocalId && IsGreeted(from))
        {
            SendReplay(from, match);
        }

        return DispatchResult.Ignored;
    }

    /// <summary>Brings a newly greeted lobby member into the current invite or game.</summary>
    public void OnMemberGreeted(PlayerId member)
    {
        if (context.Match is not { IsActive: true } match)
        {
            return;
        }

        if (match.Phase == MatchPhase.Playing)
        {
            if (match.Initiator == context.LocalId)
            {
                SendReplay(member, match);
            }

            return;
        }

        // Every player sends its own commitment, so the newcomer can check the start message against it.
        if (match.Initiator == context.LocalId)
        {
            context.Network.Send(member, new MatchProposalMessage(match.Id, match.Chain!.Anchor));
        }
        else if (match.Replies.TryGetValue(context.LocalId, out var reply) && reply.Accepted)
        {
            context.Network.Send(member, new MatchReplyMessage(match.Id, true, reply.Anchor));
        }
    }

    /// <summary>The initiator leaving ends its match; anyone else just drops out of the invite.</summary>
    public void OnPeerGone(PlayerId peer)
    {
        if (context.Match is not { IsActive: true } match)
        {
            return;
        }

        if (peer == match.Initiator)
        {
            context.EndMatch($"{context.NameOf(peer)} left the lobby, so their game ended.");
        }
        else if (match.Phase == MatchPhase.Inviting)
        {
            match.Replies.Remove(peer);
        }
    }

    private void ApplyStart(MatchState match, GameStartMessage start)
    {
        LudoGame game;
        try
        {
            if (start.Seats.Count is 0 or > SessionContext.MaxPlayers || start.Seats[0].Id != match.Initiator)
            {
                throw new DomainException("the seating is invalid.");
            }

            game = LudoGame.Start(match.Id, start.Seats.Select(s => (s.Id, s.Name)).ToArray(), start.Rules);
        }
        catch (DomainException ex)
        {
            context.EndMatch($"The game could not start: {ex.Message}");
            return;
        }

        match.Rules = start.Rules;
        match.Seats = start.Seats;
        match.Game = game;
        match.Phase = MatchPhase.Playing;
        context.AddLog($"{context.NameOf(match.Initiator)} started a game with {start.Seats.Count} players.");

        if (match.IsSeated(context.LocalId))
        {
            if (ValidateCommitments(match, start) is { } problem)
            {
                // Sitting out silently would stall everyone waiting for our dice shares.
                context.AddLog($"You could not join the game: {problem}");
                context.Network.Broadcast(new MatchLeaveMessage(match.Id));
                gameplay.DropPlayer(game, context.LocalId);
                match.Chain = null;
            }
            else
            {
                match.IsPlaying = true;
                foreach (var seat in start.Seats)
                {
                    match.Dice.Heads[seat.Id] = seat.ChainAnchor;
                }
            }
        }

        context.FlushGameEvents();
    }

    /// <summary>
    /// Every seat's commitment must match the one its owner sent us directly, otherwise a peer could give
    /// different players different commitments and steer its dice share.
    /// </summary>
    private string? ValidateCommitments(MatchState match, GameStartMessage start)
    {
        if (match.Chain is null)
        {
            return "you did not accept the invite.";
        }

        foreach (var seat in start.Seats)
        {
            var expected = seat.Id == context.LocalId
                ? match.Chain.Anchor
                : match.Replies.TryGetValue(seat.Id, out var reply) ? reply.Anchor : null;
            if (expected is null)
            {
                return $"not connected to {seat.Name}.";
            }

            if (!expected.AsSpan().SequenceEqual(seat.ChainAnchor))
            {
                return $"dice commitment mismatch for {seat.Name}.";
            }
        }

        return null;
    }

    private void SendReplay(PlayerId peer, MatchState match) =>
        context.Network.Send(peer, new MatchReplayMessage(match.Id, match.Rules, match.Seats, match.History.ToArray()));

    private void RequestReplay(MatchState match)
    {
        if (!match.AwaitingReplay)
        {
            match.AwaitingReplay = true;
            context.Network.Send(match.Initiator, new MatchSyncRequestMessage(match.Id));
        }
    }

    private void Supersede(MatchState current, string ownInviteNotice)
    {
        context.PastMatches.Add(current.Id);
        if (current.Initiator == context.LocalId)
        {
            context.AddLog(ownInviteNotice);
        }
    }

    private static void Apply(LudoGame game, MatchStep step)
    {
        switch (step.Kind)
        {
            case MatchStepKind.Roll:
                game.Roll(step.Player, new DiceValue(step.Value));
                break;
            case MatchStepKind.Move:
                game.MoveToken(step.Player, step.Value);
                break;
            case MatchStepKind.Remove:
                game.RemovePlayer(step.Player);
                break;
            default:
                throw new DomainException("Unknown step.");
        }
    }

    private MatchState? Find(Guid matchId) => context.Match is { } match && match.Id == matchId ? match : null;

    /// <summary>A message for a match we have not heard of may have overtaken the invite; one for an old match is stale.</summary>
    private DispatchResult Unknown(Guid matchId) =>
        context.PastMatches.Contains(matchId) ? DispatchResult.Ignored : DispatchResult.Deferred;

    private bool IsGreeted(PlayerId peer) => context.Peers.TryGetValue(peer, out var info) && info.IsGreeted;

    private void RequireLobby()
    {
        if (context.Stage != SessionStage.Lobby)
        {
            throw new SessionException("Join a lobby first.");
        }
    }
}
