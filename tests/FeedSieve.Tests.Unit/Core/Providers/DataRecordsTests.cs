using FeedSieve.Core.Providers;
using FluentAssertions;

namespace FeedSieve.Tests.Unit.Core.Providers;

public sealed class DataRecordsTests
{
    // ---------------------------------------------------------------------------
    // RawItem
    // ---------------------------------------------------------------------------

    [Fact]
    public void RawItem_Tags_DefaultsToEmptyCollection()
    {
        // Arrange
        var item = new RawItem
        {
            CanonicalKey = "abc",
            Title = "Hello",
        };

        // Act & Assert
        item.Tags.Should().NotBeNull();
        item.Tags.Should().BeEmpty();
    }

    [Fact]
    public void RawItem_Equality_IsStructural()
    {
        // Arrange
        var a = new RawItem { CanonicalKey = "k", Title = "t" };
        var b = new RawItem { CanonicalKey = "k", Title = "t" };

        // Act & Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void RawItem_CanOmitAllOptionalFields()
    {
        // Arrange
        var item = new RawItem
        {
            CanonicalKey = "abc",
            Title = "Hello",
        };

        // Act & Assert
        item.Url.Should().BeNull();
        item.Summary.Should().BeNull();
        item.PublishedAt.Should().BeNull();
    }

    // ---------------------------------------------------------------------------
    // FetchContext
    // ---------------------------------------------------------------------------

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

    // ---------------------------------------------------------------------------
    // FetchResult
    // ---------------------------------------------------------------------------

    [Fact]
    public void FetchResult_FetchedAt_DefaultsToNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        var result = new FetchResult
        {
            Items = [],
            ContentFingerprint = "x",
        };

        // Act
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        // Assert
        result.FetchedAt.Should().BeOnOrAfter(before)
            .And.BeOnOrBefore(after);
    }

    [Fact]
    public void FetchResult_IsNotModified_DefaultsToFalse()
    {
        // Arrange
        var result = new FetchResult
        {
            Items = [],
            ContentFingerprint = "x",
        };

        // Act & Assert
        result.IsNotModified.Should().BeFalse();
    }

    [Fact]
    public void FetchResult_NotModifiedFactory_SetsFlagAndFields()
    {
        // Arrange & Act
        var result = FetchResult.NotModified("fingerprint-v2", "\"etag-123\"");

        // Assert - check multiple fields with BeEquivalentTo anonymous object
        result.Should().BeEquivalentTo(new
        {
            IsNotModified = true,
            Items = Array.Empty<RawItem>(),
            ContentFingerprint = "fingerprint-v2",
            ETag = "\"etag-123\"",
        });
    }

    [Fact]
    public void FetchResult_NotModifiedFactory_AllowsNullETag()
    {
        // Arrange & Act
        var result = FetchResult.NotModified("fingerprint-only");

        // Assert
        result.IsNotModified.Should().BeTrue();
        result.ETag.Should().BeNull();
    }
}