using FeedSieve.Core.Providers;
using FluentAssertions;

namespace FeedSieve.Tests.Unit.Core.Providers;

public sealed partial class DataRecordsTests
{
    [Fact]
    public void FetchContext_Empty_HasAllNullState()
    {
        // Arrange
        var context = FetchContext.Empty;

        // Act & Assert
        context.LastFetchedAt.Should().BeNull();
        context.LastFingerprint.Should().BeNull();
        context.LastETag.Should().BeNull();
    }

    [Fact]
    public void FetchContext_Empty_IsSingleton()
    {
        // Act & Assert
        FetchContext.Empty.Should().BeSameAs(FetchContext.Empty);
    }

    [Fact]
    public void FetchContext_WithExpression_ProducesNewInstance()
    {
        // Arrange
        var t = DateTimeOffset.UtcNow;

        // Act
        var updated = FetchContext.Empty with { LastFetchedAt = t };

        // Assert
        updated.LastFetchedAt.Should().Be(t);
        FetchContext.Empty.LastFetchedAt.Should().BeNull(); // singleton untouched
    }
}