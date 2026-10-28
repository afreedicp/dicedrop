using Ludo.Domain.Players;

namespace Ludo.Application.Session;

/// <summary>Input limits the UI can enforce up front; the use cases check them again.</summary>
public static class SessionLimits
{
    public const int MaxNameLength = Player.MaxNameLength;

    public const int MaxChatLength = 500;

    /// <summary>Avatar photos travel to every peer in the Hello, so they stay small.</summary>
    public const int MaxPhotoBytes = 64 * 1024;

    /// <summary>Colours in the shared avatar palette.</summary>
    public const int AvatarColors = 8;

    /// <summary>How often one person may nudge the same person.</summary>
    public static readonly TimeSpan NudgeCooldown = TimeSpan.FromSeconds(10);
}
