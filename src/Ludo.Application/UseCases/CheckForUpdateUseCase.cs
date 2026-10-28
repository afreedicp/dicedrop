using Ludo.Application.Abstractions;
using Ludo.Application.Updates;

namespace Ludo.Application.UseCases;

/// <summary>Asks the release feed whether a newer version than the running one has been published.</summary>
public sealed class CheckForUpdateUseCase
{
    private readonly IReleaseFeed _feed;
    private readonly Version _current;
    private readonly IDiagnosticLog _diagnostics;

    public CheckForUpdateUseCase(IReleaseFeed feed, Version current, IDiagnosticLog? diagnostics = null)
    {
        _feed = feed;
        _current = ReleaseVersion.Normalize(current);
        _diagnostics = diagnostics ?? NullDiagnosticLog.Instance;
    }

    public Version CurrentVersion => _current;

    /// <summary>Local builds are versioned 0.0.0 and never look for updates: every release would be "newer".</summary>
    public bool IsEnabled => _current > new Version(0, 0, 0);

    /// <summary>The newer release, or null when this is the latest, the feed is unreachable, or checks are off.</summary>
    public async Task<PublishedRelease?> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return null;
        }

        PublishedRelease? latest;
        try
        {
            latest = await _feed.GetLatestAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // Offline LAN play is normal: a failed check is not worth bothering the player about.
            _diagnostics.Write("update", $"could not check for updates: {ex.Message}");
            return null;
        }

        if (latest is null || !ReleaseVersion.TryParseTag(latest.Tag, out var version))
        {
            return null;
        }

        if (version <= _current)
        {
            return null;
        }

        _diagnostics.Write("update", $"{latest.Tag} is available (running v{_current})");
        return latest;
    }
}
