using System.Buffers.Binary;
using Ludo.Application.Abstractions;
using Ludo.Domain.Gameplay;

namespace Ludo.Application.FairDice;

public static class DiceDerivation
{
    /// <summary>Combines every participant's reveal (in seat order) into one die face.</summary>
    public static DiceValue Derive(ICryptoProvider crypto, int rollIndex, IEnumerable<byte[]> revealsInSeatOrder)
    {
        using var buffer = new MemoryStream();
        Span<byte> index = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(index, rollIndex);
        buffer.Write(index);
        foreach (var reveal in revealsInSeatOrder)
        {
            buffer.Write(reveal);
        }

        var digest = crypto.Hash(buffer.ToArray());
        var number = BinaryPrimitives.ReadUInt64LittleEndian(digest);

        // 2^64 mod 6 = 4, so the bias is about 2e-19: negligible.
        return new DiceValue((int)(number % DiceValue.Faces) + 1);
    }
}
