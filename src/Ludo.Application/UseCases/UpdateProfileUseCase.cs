using Ludo.Application.Protocol;
using Ludo.Application.Session;
using Ludo.Application.Sync;

namespace Ludo.Application.UseCases;

/// <summary>Sets our avatar: a palette colour and an optional photo. Works before joining a lobby too.</summary>
public sealed class UpdateProfileUseCase
{
    private readonly SessionContext _context;
    private readonly SessionExecutor _executor;
    private readonly LobbyProtocol _lobby;

    internal UpdateProfileUseCase(SessionContext context, SessionExecutor executor, LobbyProtocol lobby)
    {
        _context = context;
        _executor = executor;
        _lobby = lobby;
    }

    /// <param name="photo">A small PNG or JPEG of at most <see cref="SessionLimits.MaxPhotoBytes"/>, or null.</param>
    public Task ExecuteAsync(int color, byte[]? photo) => _executor.RunAsync(() =>
    {
        _lobby.ProfileLocal(new PlayerProfile(color, photo));
        _context.Publish();
    });
}
