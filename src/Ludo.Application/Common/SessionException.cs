namespace Ludo.Application.Common;

/// <summary>A request the user can fix or retry, e.g. joining a full lobby or rolling out of turn.</summary>
public sealed class SessionException(string message) : Exception(message);
