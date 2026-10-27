using Ludo.Domain.Common;

namespace Ludo.Domain.Board;

/// <summary>
/// A token's position expressed as progress along its owner's path:
/// -1 is the yard, 0 is the owner's start square, and the path ends at <see cref="BoardTopology.FinishProgress"/>.
/// </summary>
public readonly record struct TokenPosition
{
    private TokenPosition(int progress) => Progress = progress;

    public int Progress { get; }

    public static TokenPosition Yard { get; } = new(-1);

    public bool IsInYard => Progress < 0;

    public static TokenPosition AtProgress(int progress) =>
        progress < 0 ? throw new DomainException("Progress cannot be negative.") : new TokenPosition(progress);
}
