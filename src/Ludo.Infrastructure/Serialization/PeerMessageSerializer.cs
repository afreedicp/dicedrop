using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Ludo.Application.Protocol;

namespace Ludo.Infrastructure.Serialization;

/// <summary>
/// JSON wire format. Polymorphism is configured here rather than with attributes so the
/// application's message types stay free of serialisation concerns.
/// </summary>
public sealed class PeerMessageSerializer
{
    private readonly JsonSerializerOptions _options;

    public PeerMessageSerializer()
    {
        var messageTypes = typeof(PeerMessage).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false } && t.IsSubclassOf(typeof(PeerMessage)))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToArray();

        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(typeInfo =>
        {
            if (typeInfo.Type != typeof(PeerMessage))
            {
                return;
            }

            typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = "$type",
                UnknownDerivedTypeHandling = System.Text.Json.Serialization.JsonUnknownDerivedTypeHandling.FailSerialization,
            };
            foreach (var type in messageTypes)
            {
                typeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(type, type.Name));
            }
        });

        _options = new JsonSerializerOptions
        {
            TypeInfoResolver = resolver,
            MaxDepth = 16,
        };
    }

    public byte[] Serialize(PeerMessage message) => JsonSerializer.SerializeToUtf8Bytes(message, _options);

    /// <exception cref="JsonException">The payload is not a valid message.</exception>
    public PeerMessage Deserialize(ReadOnlySpan<byte> payload) =>
        JsonSerializer.Deserialize<PeerMessage>(payload, _options)
        ?? throw new JsonException("Empty message.");
}
