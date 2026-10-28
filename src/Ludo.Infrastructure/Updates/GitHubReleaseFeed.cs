using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Ludo.Application.Abstractions;

namespace Ludo.Infrastructure.Updates;

/// <summary>Reads the latest release of a GitHub repository through the public REST API; no token needed.</summary>
public sealed class GitHubReleaseFeed : IReleaseFeed, IDisposable
{
    private readonly HttpClient _http;
    private readonly Uri _latest;

    public GitHubReleaseFeed(string owner, string repository, string userAgent)
    {
        // "latest" skips drafts and pre-releases, which is what players should be offered.
        _latest = new Uri($"https://api.github.com/repos/{owner}/{repository}/releases/latest");
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent); // GitHub rejects requests without one
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public async Task<PublishedRelease?> GetLatestAsync(CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(_latest, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null; // nothing released yet
        }

        response.EnsureSuccessStatusCode();
        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var json = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken).ConfigureAwait(false);

        var root = json.RootElement;
        if (!root.TryGetProperty("tag_name", out var tag) || tag.ValueKind != JsonValueKind.String ||
            !root.TryGetProperty("html_url", out var url) || url.ValueKind != JsonValueKind.String ||
            !Uri.TryCreate(url.GetString(), UriKind.Absolute, out var page) || page.Scheme != Uri.UriSchemeHttps)
        {
            return null;
        }

        return new PublishedRelease(tag.GetString()!, page);
    }

    public void Dispose() => _http.Dispose();
}
