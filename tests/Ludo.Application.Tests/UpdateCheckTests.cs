using Ludo.Application.Abstractions;
using Ludo.Application.Updates;
using Ludo.Application.UseCases;

namespace Ludo.Application.Tests;

public sealed class UpdateCheckTests
{
    private static readonly Uri Page = new("https://github.com/i-oms/dicedrop/releases/tag/v1.3.0");

    [Theory]
    [InlineData("v1.2.0", 1, 2, 0)]
    [InlineData("v10.0.3", 10, 0, 3)]
    [InlineData("v0.0.1", 0, 0, 1)]
    public void Accepts_the_release_tag_format(string tag, int major, int minor, int patch)
    {
        Assert.True(ReleaseVersion.TryParseTag(tag, out var version));
        Assert.Equal(new Version(major, minor, patch), version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("1.2.0")]
    [InlineData("V1.2.0")]
    [InlineData("v1.2")]
    [InlineData("v01.2.0")]
    [InlineData("v1.2.0-beta")]
    [InlineData("v1.2.0\n")]
    [InlineData("v999999999.0.0")]
    public void Rejects_anything_else(string? tag) => Assert.False(ReleaseVersion.TryParseTag(tag, out _));

    [Theory]
    [InlineData("v1.3.0", true)]
    [InlineData("v1.2.1", true)]
    [InlineData("v2.0.0", true)]
    [InlineData("v1.2.0", false)]
    [InlineData("v1.1.9", false)]
    [InlineData("nightly", false)]
    public async Task Offers_only_a_newer_release(string latest, bool offered)
    {
        var check = new CheckForUpdateUseCase(new FakeFeed(new PublishedRelease(latest, Page)), new Version(1, 2, 0, 0));

        var release = await check.ExecuteAsync();

        Assert.Equal(offered, release is not null);
    }

    [Fact]
    public async Task Local_builds_never_check()
    {
        var feed = new FakeFeed(new PublishedRelease("v9.0.0", Page));
        var check = new CheckForUpdateUseCase(feed, new Version(0, 0, 0, 0));

        Assert.False(check.IsEnabled);
        Assert.Null(await check.ExecuteAsync());
        Assert.Equal(0, feed.Calls);
    }

    [Fact]
    public async Task An_unreachable_feed_means_no_update()
    {
        var check = new CheckForUpdateUseCase(new FakeFeed(new HttpRequestException("offline")), new Version(1, 0, 0));

        Assert.Null(await check.ExecuteAsync());
    }

    [Fact]
    public async Task No_releases_yet_means_no_update()
    {
        var check = new CheckForUpdateUseCase(new FakeFeed((PublishedRelease?)null), new Version(1, 0, 0));

        Assert.Null(await check.ExecuteAsync());
    }

    private sealed class FakeFeed : IReleaseFeed
    {
        private readonly PublishedRelease? _release;
        private readonly Exception? _error;

        public FakeFeed(PublishedRelease? release) => _release = release;

        public FakeFeed(Exception error) => _error = error;

        public int Calls { get; private set; }

        public Task<PublishedRelease?> GetLatestAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return _error is null ? Task.FromResult(_release) : Task.FromException<PublishedRelease?>(_error);
        }
    }
}
