using Ludo.Domain.Common;

namespace Ludo.Domain.Board;

/// <summary>
/// Shape of a Ludo board generalised to any number of players. The board has one arm of
/// <see cref="SquaresPerArm"/> track squares per player, but never fewer than the classic four, so
/// two or three players race on the traditional 52-square board from opposite or adjacent corners.
/// A token runs one lap minus one square, turns into its own home column, and finishes.
/// </summary>
public sealed class BoardTopology
{
    public const int MinPlayers = 2;
    public const int MinArms = 4;
    public const int SquaresPerArm = 13;
    public const int HomeColumnLength = 5;
    public const int StarOffset = 8;

    public BoardTopology(int playerCount)
    {
        if (playerCount < MinPlayers)
        {
            throw new DomainException($"A game needs at least {MinPlayers} players.");
        }

        PlayerCount = playerCount;
        ArmCount = Math.Max(MinArms, playerCount);
        MainTrackLength = ArmCount * SquaresPerArm;
    }

    public int PlayerCount { get; }

    public int ArmCount { get; }

    public int MainTrackLength { get; }

    public int LastTrackProgress => MainTrackLength - 2;

    public int FirstHomeColumnProgress => MainTrackLength - 1;

    public int FinishProgress => FirstHomeColumnProgress + HomeColumnLength;

    /// <summary>Arm a seat plays from. Seats spread evenly, so two players sit in opposite corners.</summary>
    public int ArmOf(int seat)
    {
        if (seat < 0 || seat >= PlayerCount)
        {
            throw new DomainException($"Seat {seat} does not exist.");
        }

        return seat * ArmCount / PlayerCount;
    }

    public int StartSquareOf(int seat) => ArmOf(seat) * SquaresPerArm;

    public TokenZone ZoneOf(TokenPosition position) => position.Progress switch
    {
        < 0 => TokenZone.Yard,
        var p when p <= LastTrackProgress => TokenZone.Track,
        var p when p < FinishProgress => TokenZone.HomeColumn,
        _ => TokenZone.Finished,
    };

    /// <summary>Absolute index on the shared loop, or null when the token is not on the loop.</summary>
    public int? TrackSquareOf(int seat, TokenPosition position) =>
        ZoneOf(position) == TokenZone.Track
            ? (StartSquareOf(seat) + position.Progress) % MainTrackLength
            : null;

    /// <summary>0-based step inside the owner's home column.</summary>
    public int HomeStepOf(TokenPosition position) =>
        ZoneOf(position) == TokenZone.HomeColumn
            ? position.Progress - FirstHomeColumnProgress
            : throw new DomainException("Token is not in its home column.");

    /// <summary>Start squares and star squares protect tokens from capture.</summary>
    public bool IsSafeSquare(int square)
    {
        var offset = square % SquaresPerArm;
        return offset == 0 || offset == StarOffset;
    }
}
