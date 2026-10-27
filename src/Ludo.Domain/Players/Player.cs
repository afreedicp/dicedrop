using Ludo.Domain.Common;

namespace Ludo.Domain.Players;

public sealed class Player
{
    public const int MaxNameLength = 24;

    internal Player(PlayerId id, string name, int seat)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw new DomainException("Player name is required.");
        }

        Id = id;
        Name = trimmed.Length > MaxNameLength ? trimmed[..MaxNameLength] : trimmed;
        Seat = seat;
        Status = PlayerStatus.Playing;
    }

    public PlayerId Id { get; }

    public string Name { get; }

    public int Seat { get; }

    public PlayerStatus Status { get; private set; }

    /// <summary>Finishing position (1 = winner); null while still racing or if the player left early.</summary>
    public int? Rank { get; private set; }

    internal void MarkFinished(int rank)
    {
        Status = PlayerStatus.Finished;
        Rank = rank;
    }

    internal void MarkLeft() => Status = PlayerStatus.Left;
}
