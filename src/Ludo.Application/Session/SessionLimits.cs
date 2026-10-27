using Ludo.Domain.Players;

namespace Ludo.Application.Session;

/// <summary>Input limits the UI can enforce up front; the use cases check them again.</summary>
public static class SessionLimits
{
    public const int MaxNameLength = Player.MaxNameLength;

    public const int MaxChatLength = 500;
}
