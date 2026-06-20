using FeedSieve.Core.Providers;
using FluentAssertions;

namespace FeedSieve.Tests.Unit.Core.Providers;

public sealed partial class DataRecordsTests
{
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