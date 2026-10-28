namespace Ludo.Application.Abstractions;

/// <summary>Where published versions of the game are listed, such as the project's GitHub releases.</summary>
public interface IReleaseFeed
{
    /// <summary>The newest published release, or null if there is none. Throws when the feed can't be reached.</summary>
    Task<PublishedRelease?> GetLatestAsync(CancellationToken cancellationToken);
}

/// <param name="Tag">The release's tag as published, e.g. "v1.2.0".</param>
/// <param name="Page">The web page players download the release from.</param>
public sealed record PublishedRelease(string Tag, Uri Page);
