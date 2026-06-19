using FeedSieve.Core.Providers;
using FeedSieve.Tests.Unit.TestDoubles;
using FluentAssertions;

namespace FeedSieve.Tests.Unit.Core.Providers;


public sealed class ProviderRegistryTests
{
    [Fact]
    public void All_ReturnsAllRegisteredProviders()
    {
        // Arrange
        var rss = new FakeRssProvider();
        var webhook = new FakeWebhookProvider();

        // Act
        var registry = new ProviderRegistry([rss, webhook]);

        // Assert
        registry.All.Should().BeEquivalentTo(new ISourceProvider[] { rss, webhook });
    }

    [Fact]
    public void All_IsEmpty_WhenNoProvidersRegistered()
    {
        // Act
        var registry = new ProviderRegistry([]);

        // Assert
        registry.All.Should().BeEmpty();
    }

    [Fact]
    public void Get_ReturnsProviderById()
    {
        // Arrange
        var rss = new FakeRssProvider { ProviderId = "rss" };
        var webhook = new FakeWebhookProvider { ProviderId = "webhook" };
        var registry = new ProviderRegistry([rss, webhook]);

        // Act & Assert
        registry.Get("rss").Should().BeSameAs(rss);
        registry.Get("webhook").Should().BeSameAs(webhook);
    }

    [Theory]
    [InlineData("rss")]
    [InlineData("RSS")]
    [InlineData("Rss")]
    public void Get_IsCaseInsensitive(string lookupId)
    {
        // Arrange
        var provider = new FakeRssProvider { ProviderId = "rss" };
        var registry = new ProviderRegistry([provider]);

        // Act & Assert
        registry.Get(lookupId).Should().BeSameAs(provider);
    }

    [Fact]
    public void Get_ThrowsWhenProviderNotRegistered()
    {
        // Arrange
        var registry = new ProviderRegistry([new FakeRssProvider()]);

        // Act
        var act = () => registry.Get("missing");

        // Assert
        act.Should().Throw<KeyNotFoundException>()
           .WithMessage("*missing*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Get_ThrowsForInvalidId(string? id)
    {
        // Arrange
        var registry = new ProviderRegistry([new FakeRssProvider()]);

        // Act
        var act = () => registry.Get(id!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryGet_ReturnsTrueAndProvider_WhenFound()
    {
        // Arrange
        var provider = new FakeRssProvider();
        var registry = new ProviderRegistry([provider]);

        // Act
        var found = registry.TryGet(provider.ProviderId, out var resolved);

        // Assert
        found.Should().BeTrue();
        resolved.Should().BeSameAs(provider);
    }

    [Fact]
    public void TryGet_ReturnsFalse_WhenNotFound()
    {
        // Arrange
        var registry = new ProviderRegistry([new FakeRssProvider()]);

        // Act
        var found = registry.TryGet("missing", out var resolved);

        // Assert
        found.Should().BeFalse();
        resolved.Should().BeNull();
    }

    [Fact]
    public void Constructor_Throws_WhenProviderIdsCollide()
    {
        // Arrange
        var first = new FakeRssProvider { ProviderId = "duplicate" };
        var second = new FakeWebhookProvider { ProviderId = "duplicate" };

        // Act
        var act = () => new ProviderRegistry([first, second]);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*duplicate*");
    }

    [Fact]
    public void Constructor_Throws_WhenProviderIdsCollideCaseInsensitively()
    {
        // Arrange
        var first = new FakeRssProvider { ProviderId = "rss" };
        var second = new FakeWebhookProvider { ProviderId = "RSS" };

        // Act
        var act = () => new ProviderRegistry([first, second]);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_Throws_WhenConfigurationTypesCollide()
    {
        // Arrange
        var first = new FakeRssProvider { ProviderId = "a", ConfigurationType = typeof(FakeRssConfiguration) };
        var second = new FakeRssProvider { ProviderId = "b", ConfigurationType = typeof(FakeRssConfiguration) };

        // Act
        var act = () => new ProviderRegistry([first, second]);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*configuration type*");
    }

    [Fact]
    public void GetForConfiguration_RoutesByConcreteType()
    {
        // Arrange
        var rss = new FakeRssProvider();
        var webhook = new FakeWebhookProvider();
        var registry = new ProviderRegistry([rss, webhook]);

        // Act & Assert
        registry.GetForConfiguration(typeof(FakeRssConfiguration)).Should().BeSameAs(rss);
        registry.GetForConfiguration(typeof(FakeWebhookConfiguration)).Should().BeSameAs(webhook);
    }

    [Fact]
    public void GetForConfiguration_Throws_WhenTypeUnknown()
    {
        // Arrange
        var registry = new ProviderRegistry([new FakeRssProvider()]);

        // Act
        var act = () => registry.GetForConfiguration(typeof(FakeWebhookConfiguration));

        // Assert
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void Constructor_Throws_WhenProvidersIsNull()
    {
        // Act
        var act = () => new ProviderRegistry(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}