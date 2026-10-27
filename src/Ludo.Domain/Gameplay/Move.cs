using Ludo.Domain.Board;

namespace Ludo.Domain.Gameplay;

public sealed record TokenRef(int Seat, int TokenIndex);

public sealed record Move(int TokenIndex, TokenPosition From, TokenPosition To, IReadOnlyList<TokenRef> Captures);
