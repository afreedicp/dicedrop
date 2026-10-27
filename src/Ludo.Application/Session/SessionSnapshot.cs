namespace Ludo.Application.Session;

// Read models handed to the presenter. They carry plain values only, so the UI never touches the domain.

public enum SessionStage
{
    Idle,
    Lobby,
}

public enum MatchPhaseView
{
    Inviting,
    Playing,
    Finished,
    Stopped,
}

public enum InviteAnswer
{
    Pending,
    Accepted,
    Declined,
}

public enum GamePhaseView
{
    WaitingForRoll,
    Rolling,
    WaitingForMove,
    Over,
}

public enum TokenLocation
{
    Yard,
    Track,
    HomeColumn,
    Finished,
}

public enum SeatStanding
{
    Playing,
    Finished,
    Left,
}

public sealed record SessionSnapshot(
    SessionStage Stage,
    string LocalName,
    bool IsHost,
    IReadOnlyList<MemberView> Members,
    bool CanProposeGame,
    IReadOnlyList<string> JoinAddresses,
    MatchView? Match,
    IReadOnlyList<string> Log,
    IReadOnlyList<ChatLineView> Chat)
{
    public static SessionSnapshot Empty { get; } = new(SessionStage.Idle, string.Empty, false, [], false, [], null, [], []);
}

/// <param name="InGame">Holds a seat in the running game and has not left it.</param>
public sealed record MemberView(string Name, bool IsHost, bool IsLocal, bool IsReady, bool InGame);

/// <param name="Sender">The sender's name when the line was sent.</param>
public sealed record ChatLineView(string Sender, string Text, bool IsLocal);

/// <summary>Someone in the lobby asked who wants to play.</summary>
public sealed record InviteView(Guid MatchId, string From);

/// <param name="IsPlaying">We hold a seat and have not left; false for spectators.</param>
/// <param name="CanStart">We are the initiator and enough players have said yes.</param>
public sealed record MatchView(
    Guid Id,
    string InitiatorName,
    bool IsInitiator,
    MatchPhaseView Phase,
    InviteAnswer LocalAnswer,
    bool IsPlaying,
    IReadOnlyList<MatchMemberView> Invitees,
    bool CanStart,
    string? EndReason,
    GameStateView? Game);

public sealed record MatchMemberView(string Name, bool IsInitiator, bool IsLocal, InviteAnswer Answer);

/// <param name="ArmCount">Arms drawn on the board: one per player, never fewer than four.</param>
public sealed record BoardLayoutView(
    int PlayerCount,
    int ArmCount,
    int MainTrackLength,
    int SquaresPerArm,
    int HomeColumnLength,
    int StarOffset,
    int TokensPerPlayer);

/// <param name="Progress">Steps along the owner's path: -1 in the yard, 0 on the start square, up to the finish.</param>
public sealed record TokenView(int Index, TokenLocation Location, int Progress, int TrackSquare, int HomeStep, bool CanMove);

/// <param name="Arm">Board arm this seat plays from.</param>
public sealed record SeatView(
    int Seat,
    int Arm,
    string Name,
    SeatStanding Standing,
    int? Rank,
    bool IsLocal,
    bool IsCurrent,
    IReadOnlyList<TokenView> Tokens);

/// <param name="RollCount">Rolls made so far, so the UI can tell a new roll from a repeat of the same face.</param>
public sealed record GameStateView(
    BoardLayoutView Board,
    IReadOnlyList<SeatView> Seats,
    int CurrentSeat,
    GamePhaseView Phase,
    bool IsLocalTurn,
    int? LastRoll,
    int RollCount,
    IReadOnlyList<string> WaitingFor);
