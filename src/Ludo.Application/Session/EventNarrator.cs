using Ludo.Domain.Common;
using Ludo.Domain.Gameplay;
using Ludo.Domain.Players;

namespace Ludo.Application.Session;

internal static class EventNarrator
{
    public static string? Describe(IDomainEvent domainEvent, LudoGame game)
    {
        string Name(PlayerId id) => game.FindPlayer(id)?.Name ?? id.ToString();

        return domainEvent switch
        {
            DiceRolled e => $"{Name(e.Player)} rolled a {e.Value}.",
            TokenMoved e when e.From.IsInYard => $"{Name(e.Player)} brought token {e.TokenIndex + 1} out.",
            TokenMoved e => $"{Name(e.Player)} moved token {e.TokenIndex + 1}.",
            TokenCaptured e => $"{Name(e.By)} captured a token from {Name(e.Victim)}!",
            TokenReachedHome e => $"{Name(e.Player)} brought token {e.TokenIndex + 1} home.",
            NoMovesAvailable e => $"{Name(e.Player)} has no legal move.",
            TurnForfeited e => $"{Name(e.Player)} loses the turn: {e.Reason}.",
            ExtraTurnGranted e => $"{Name(e.Player)} rolls again.",
            PlayerFinished e => $"{Name(e.Player)} finished in place {e.Rank}.",
            PlayerLeft e => $"{Name(e.Player)} left the game.",
            GameEnded => "Game over.",
            _ => null,
        };
    }
}
