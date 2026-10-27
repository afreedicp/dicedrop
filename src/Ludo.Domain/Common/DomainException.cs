namespace Ludo.Domain.Common;

/// <summary>Raised when an operation would violate a game invariant or rule.</summary>
public sealed class DomainException(string message) : Exception(message);
