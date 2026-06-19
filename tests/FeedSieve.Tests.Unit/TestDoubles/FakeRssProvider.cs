using FeedSieve.Core.Providers;

namespace FeedSieve.Tests.Unit.TestDoubles;

/// <summary>
/// Test-only provider. Exposes a configurable <see cref="FetchAsync"/> hook so individual
/// tests can assert call shape without standing up infrastructure.
/// </summary>
internal sealed class FakeRssProvider : ISourceProvider
{
    public string ProviderId { get; init; } = "fake-rss";
    public string DisplayName { get; init; } = "Fake RSS";

    public SourceProviderCapabilities Capabilities { get; init; } =
        SourceProviderCapabilities.SupportsScheduledFetch;

    public Type ConfigurationPageType { get; init; } = typeof(object);
    public Type ConfigurationType { get; init; } = typeof(FakeRssConfiguration);

    public Func<SourceConfiguration, FetchContext, CancellationToken, Task<FetchResult>>? FetchHandler
    { get; init; }

    public Task<FetchResult> FetchAsync(
        SourceConfiguration configuration,
        FetchContext context,
        CancellationToken cancellationToken)
    {
        if (FetchHandler is not null)
        {
            return FetchHandler(configuration, context, cancellationToken);
        }

        return Task.FromResult(new FetchResult
        {
            Items = [],
            ContentFingerprint = "fake",
        });
    }
}