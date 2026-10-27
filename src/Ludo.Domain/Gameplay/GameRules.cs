using Ludo.Domain.Common;

namespace Ludo.Domain.Gameplay;

public sealed record GameRules
{
    public static GameRules Classic { get; } = new();

    public int TokensPerPlayer { get; init; } = 4;

    /// <summary>Roll needed to bring a token out of the yard.</summary>
    public int EntryRoll { get; init; } = 6;

    /// <summary>Rolling this many sixes in a row forfeits the turn; 0 disables the rule.</summary>
    public int MaxConsecutiveSixes { get; init; } = 3;

    public bool ExtraTurnOnSix { get; init; } = true;

    public bool ExtraTurnOnCapture { get; init; } = true;

    public bool ExtraTurnOnFinish { get; init; } = true;

    public void Validate()
    {
        if (TokensPerPlayer is < 1 or > 6)
        {
            throw new DomainException("Each player needs between 1 and 6 tokens.");
        }

        if (EntryRoll is < 1 or > DiceValue.Faces)
        {
            throw new DomainException("Entry roll must be a die face.");
        }

        if (MaxConsecutiveSixes < 0)
        {
            throw new DomainException("Consecutive six limit cannot be negative.");
        }
    }
}
