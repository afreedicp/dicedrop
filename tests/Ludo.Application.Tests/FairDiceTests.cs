using Ludo.Application.FairDice;
using Ludo.Infrastructure.Crypto;

namespace Ludo.Application.Tests;

public sealed class FairDiceTests
{
    private readonly Sha256CryptoProvider _crypto = new();

    [Fact]
    public void Each_reveal_hashes_to_the_previous_value()
    {
        var chain = HashChain.Create(_crypto, length: 50);

        var previous = chain.Anchor;
        for (var i = 1; i <= chain.Length; i++)
        {
            var reveal = chain.Reveal(i);
            Assert.True(HashChain.Verifies(_crypto, previous, reveal));
            previous = reveal;
        }
    }

    [Fact]
    public void A_forged_reveal_is_rejected()
    {
        var chain = HashChain.Create(_crypto, length: 5);

        Assert.False(HashChain.Verifies(_crypto, chain.Anchor, _crypto.RandomBytes(HashChain.ValueLength)));
        Assert.False(HashChain.Verifies(_crypto, chain.Anchor, chain.Reveal(2)));
    }

    [Fact]
    public void Derived_dice_are_roughly_uniform()
    {
        var counts = new int[7];
        for (var roll = 1; roll <= 6000; roll++)
        {
            counts[DiceDerivation.Derive(_crypto, roll, [_crypto.RandomBytes(32), _crypto.RandomBytes(32)]).Value]++;
        }

        Assert.All(counts.Skip(1), c => Assert.InRange(c, 850, 1150));
    }
}
