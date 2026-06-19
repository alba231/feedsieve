using FeedSieve.Core.Providers;
using FeedSieve.Tests.Unit.TestDoubles;
using FluentAssertions;
using System.Text.Json;

namespace FeedSieve.Tests.Unit.Core.Providers;

public sealed class SourceConfigurationSerializerTests
{
    private static SourceConfigurationSerializer CreateSerializer(params ISourceProvider[] providers)
        => new(new ProviderRegistry(providers.Length == 0
            ? [new FakeRssProvider(), new FakeWebhookProvider()]
            : providers));

    private static FakeRssConfiguration SampleRssConfig() => new()
    {
        Id = "src-001",
        ProviderId = "fake-rss",
        DisplayName = "Hacker News",
        RefreshInterval = TimeSpan.FromHours(1),
        FeedUrl = new Uri("https://news.ycombinator.com/rss"),
        UserAgent = "FeedSieve/1.0",
    };

    private static FakeWebhookConfiguration SampleWebhookConfig() => new()
    {
        Id = "src-002",
        ProviderId = "fake-webhook",
        DisplayName = "Discord notifications",
        WebhookUrl = new Uri("https://example.com/hook"),
        SecretToken = "shh",
    };

    [Fact]
    public void SerializeDeserialize_PreservesConcreteType_ForRssConfig()
    {
        // Arrange
        var serializer = CreateSerializer();
        var original = SampleRssConfig();

        // Act
        var json = serializer.Serialize(original);
        var roundtripped = serializer.Deserialize(json);

        // Assert
        roundtripped.Should().BeOfType<FakeRssConfiguration>();
        roundtripped.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void SerializeDeserialize_PreservesConcreteType_ForWebhookConfig()
    {
        // Arrange
        var serializer = CreateSerializer();
        var original = SampleWebhookConfig();

        // Act
        var json = serializer.Serialize(original);
        var roundtripped = serializer.Deserialize(json);

        // Assert
        roundtripped.Should().BeOfType<FakeWebhookConfiguration>();
        roundtripped.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void Serialize_EmitsProviderId_AsTypeDiscriminator()
    {
        // Arrange
        var serializer = CreateSerializer();
        var config = SampleRssConfig();

        // Act
        var json = serializer.Serialize(config);

        // Assert
        using var doc = JsonDocument.Parse(json);
        doc.RootElement
            .GetProperty(SourceConfigurationSerializer.TypeDiscriminatorPropertyName)
            .GetString()
            .Should().Be("fake-rss");
    }

    [Fact]
    public void Deserialize_DiscriminatorRoutesToCorrectConcreteType_WhenMultipleRegistered()
    {
        // Arrange
        var serializer = CreateSerializer();

        // Act
        var rssJson = serializer.Serialize(SampleRssConfig());
        var webhookJson = serializer.Serialize(SampleWebhookConfig());

        // Assert
        serializer.Deserialize(rssJson).Should().BeOfType<FakeRssConfiguration>();
        serializer.Deserialize(webhookJson).Should().BeOfType<FakeWebhookConfiguration>();
    }

    [Fact]
    public void Deserialize_ThrowsForUnknownDiscriminator()
    {
        // Arrange - Only the RSS provider is registered; webhook discriminator must fail.
        var serializer = CreateSerializer(new FakeRssProvider());
        var json = """{"$type":"fake-webhook","Id":"x","ProviderId":"fake-webhook","DisplayName":"x"}""";

        // Act
        var act = () => serializer.Deserialize(json);

        // Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Deserialize_ThrowsWhenDiscriminatorMissing()
    {
        // Arrange
        var serializer = CreateSerializer();
        var json = """{"Id":"x","ProviderId":"fake-rss","DisplayName":"x"}""";

        // Act
        var act = () => serializer.Deserialize(json);

        // Assert
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Serialize_OmitsNullOptionalFields()
    {
        // Arrange
        var serializer = CreateSerializer();
        var config = SampleRssConfig() with { UserAgent = null, RefreshInterval = null };

        // Act
        var json = serializer.Serialize(config);

        // Assert
        json.Should().NotContain("UserAgent");
        json.Should().NotContain("RefreshInterval");
    }

    [Fact]
    public void Constructor_ThrowsWhenRegistryIsNull()
    {
        // Arrange / Act
        var act = () => new SourceConfigurationSerializer(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Deserialize_ThrowsForInvalidInput(string? json)
    {
        // Arrange
        var serializer = CreateSerializer();

        // Act
        var act = () => serializer.Deserialize(json!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}