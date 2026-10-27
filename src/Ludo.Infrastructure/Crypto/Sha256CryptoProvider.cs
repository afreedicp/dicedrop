using System.Security.Cryptography;
using Ludo.Application.Abstractions;

namespace Ludo.Infrastructure.Crypto;

public sealed class Sha256CryptoProvider : ICryptoProvider
{
    public byte[] RandomBytes(int count) => RandomNumberGenerator.GetBytes(count);

    public byte[] Hash(ReadOnlySpan<byte> data) => SHA256.HashData(data);
}
