using FeedSieve.Core.Providers;

namespace FeedSieve.Tests.Unit.TestDoubles;

internal sealed class FakeWebhookProvider : ISourceProvider
{
    public string ProviderId { get; init; } = "fake-webhook";
    public string DisplayName { get; init; } = "Fake Webhook";

    public SourceProviderCapabilities Capabilities { get; init; } =
        SourceProviderCapabilities.SupportsPushIngest | SourceProviderCapabilities.RequiresAuth;

    public Type ConfigurationPageType { get; init; } = typeof(object);
    public Type ConfigurationType { get; init; } = typeof(FakeWebhookConfiguration);

    public Task<FetchResult> FetchAsync(
        SourceConfiguration configuration,
        FetchContext context,
        CancellationToken cancellationToken) =>
        Task.FromResult(new FetchResult
        {
            Items = [],
            ContentFingerprint = "fake-webhook",
        });
}