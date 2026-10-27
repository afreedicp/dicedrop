using Ludo.Domain.Common;

namespace Ludo.Domain.Gameplay;

public readonly record struct DiceValue
{
    public const int Faces = 6;

    public DiceValue(int value)
    {
        if (value is < 1 or > Faces)
        {
            throw new DomainException($"A die shows 1 to {Faces}, not {value}.");
        }

        Value = value;
    }

    public int Value { get; }

    public bool IsSix => Value == Faces;
}
