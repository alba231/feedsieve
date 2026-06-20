using FeedSieve.Core.Providers;

namespace FeedSieve.Tests.Unit.TestDoubles;

/// <summary>
/// Test-only second configuration type. Having two distinct types lets us verify the
/// serializer routes to the correct concrete type by discriminator, not by guessing.
/// </summary>
internal sealed record FakeWebhookConfiguration : SourceConfiguration
{
    public required Uri WebhookUrl { get; init; }
    public required string SecretToken { get; init; }
}