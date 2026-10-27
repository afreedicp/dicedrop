namespace Ludo.Application.Abstractions;

public interface ICryptoProvider
{
    byte[] RandomBytes(int count);

    /// <summary>A collision-resistant 32-byte hash.</summary>
    byte[] Hash(ReadOnlySpan<byte> data);
}
