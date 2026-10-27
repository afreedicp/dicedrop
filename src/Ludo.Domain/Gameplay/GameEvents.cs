using Ludo.Domain.Board;
using Ludo.Domain.Common;
using Ludo.Domain.Players;

namespace Ludo.Domain.Gameplay;

public sealed record DiceRolled(PlayerId Player, int Value) : IDomainEvent;

public sealed record TokenMoved(PlayerId Player, int TokenIndex, TokenPosition From, TokenPosition To) : IDomainEvent;

public sealed record TokenCaptured(PlayerId By, PlayerId Victim, int VictimToken) : IDomainEvent;

public sealed record TokenReachedHome(PlayerId Player, int TokenIndex) : IDomainEvent;

public sealed record NoMovesAvailable(PlayerId Player) : IDomainEvent;

public sealed record TurnForfeited(PlayerId Player, string Reason) : IDomainEvent;

public sealed record ExtraTurnGranted(PlayerId Player) : IDomainEvent;

public sealed record TurnChanged(PlayerId Player, int Seat) : IDomainEvent;

public sealed record PlayerFinished(PlayerId Player, int Rank) : IDomainEvent;

public sealed record PlayerLeft(PlayerId Player) : IDomainEvent;

public sealed record GameEnded(IReadOnlyList<PlayerId> Ranking) : IDomainEvent;
