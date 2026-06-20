using FeedSieve.Core.Providers;
using FluentAssertions;

namespace FeedSieve.Tests.Unit.Core.Providers;

public sealed partial class DataRecordsTests
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
}