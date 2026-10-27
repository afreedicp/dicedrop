using Ludo.Application.Abstractions;
using Ludo.Application.Common;

namespace Ludo.Application.FairDice;

/// <summary>
/// Per-peer dice commitment. A random seed is hashed repeatedly; the final hash (the anchor, v0) is
/// published before the game. Roll i reveals v_i, which anyone can check because Hash(v_i) == v_(i-1).
/// Every value is fixed before play starts, so nobody, not even whoever reveals last, can steer a roll,
/// and nobody can compute a roll until every peer has revealed its share.
/// </summary>
public sealed class HashChain
{
    public const int DefaultLength = 4096;
    public const int ValueLength = 32;

    private readonly byte[][] _values;

    private HashChain(byte[][] values) => _values = values;

    public int Length => _values.Length - 1;

    public byte[] Anchor => _values[0];

    public static HashChain Create(ICryptoProvider crypto, int length = DefaultLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 1);

        var values = new byte[length + 1][];
        values[length] = crypto.RandomBytes(ValueLength);
        for (var i = length - 1; i >= 0; i--)
        {
            values[i] = crypto.Hash(values[i + 1]);
        }

        return new HashChain(values);
    }

    public static bool Verifies(ICryptoProvider crypto, byte[] previous, byte[] revealed) =>
        revealed.Length == ValueLength && crypto.Hash(revealed).AsSpan().SequenceEqual(previous);

    public byte[] Reveal(int rollIndex) =>
        rollIndex >= 1 && rollIndex <= Length
            ? _values[rollIndex]
            : throw new SessionException("This game has used up its dice commitments.");
}
