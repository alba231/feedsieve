using FeedSieve.Core.Providers;

namespace FeedSieve.Tests.Unit.TestDoubles;

/// <summary>
/// Test-only concrete <see cref="SourceConfiguration"/> for exercising the framework
/// without depending on the real RSS provider project.
/// </summary>
internal sealed record FakeRssConfiguration : SourceConfiguration
{
    public required Uri FeedUrl { get; init; }
    public string? UserAgent { get; init; }
}