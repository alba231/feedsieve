using FeedSieve.Core.Providers;
using FluentAssertions;

namespace FeedSieve.Tests.Unit.Core.Providers;

public sealed class ItemIdGeneratorTests
{
    [Fact]
    public void Compute_IsDeterministic_ForSameInputs()
    {
        // Arrange & Act
        var first = ItemIdGenerator.Compute("source-1", "guid-abc");
        var second = ItemIdGenerator.Compute("source-1", "guid-abc");

        // Assert
        first.Should().Be(second);
    }

    [Fact]
    public void Compute_Produces24CharLowercaseHex()
    {
        // Arrange & Act
        var id = ItemIdGenerator.Compute("source-1", "some-key");

        // Assert
        id.Should().HaveLength(24);
        id.Should().MatchRegex("^[0-9a-f]+$");
    }

    [Fact]
    public void Compute_DistinguishesDifferentSourceIds()
    {
        // Arrange & Act
        var a = ItemIdGenerator.Compute("source-1", "key");
        var b = ItemIdGenerator.Compute("source-2", "key");

        // Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void Compute_DistinguishesDifferentCanonicalKeys()
    {
        // Arrange & Act
        var a = ItemIdGenerator.Compute("source", "key-1");
        var b = ItemIdGenerator.Compute("source", "key-2");

        // Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void Compute_UsesLengthPrefixedEncoding_ToPreventBoundaryCollisions()
    {
        // Arrange - Without length-prefixing, ("ab","cde") and ("abc","de") would hash the same string "abcde".
        var a = ItemIdGenerator.Compute("ab", "cde");
        var b = ItemIdGenerator.Compute("abc", "de");

        // Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void Compute_HandlesUnicodeInputs()
    {
        // Arrange & Act
        var a = ItemIdGenerator.Compute("джерело", "ключ");
        var b = ItemIdGenerator.Compute("джерело", "ключ");

        // Assert
        a.Should().Be(b);
        a.Should().HaveLength(24);
    }

    [Fact]
    public void Compute_HandlesInputs_LargerThanStackallocThreshold()
    {
        // Arrange
        var longInput = new string('x', 1024);

        // Act
        var a = ItemIdGenerator.Compute(longInput, longInput);
        var b = ItemIdGenerator.Compute(longInput, longInput);

        // Assert
        a.Should().Be(b);
        a.Should().HaveLength(24);
    }

    [Theory]
    [InlineData(null, "key")]
    [InlineData("", "key")]
    [InlineData("   ", "key")]
    [InlineData("source", null)]
    [InlineData("source", "")]
    [InlineData("source", "   ")]
    public void Compute_ThrowsForInvalidInputs(string? sourceId, string? canonicalKey)
    {
        // Act
        var act = () => ItemIdGenerator.Compute(sourceId!, canonicalKey!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Compute_HasAcceptableCollisionRate_OnSyntheticInputs()
    {
        // Arrange
        const int sampleSize = 10_000;
        var ids = new HashSet<string>(sampleSize);

        // Act
        for (var i = 0; i < sampleSize; i++)
        {
            ids.Add(ItemIdGenerator.Compute("source", $"item-{i}"));
        }

        // Assert
        ids.Should().HaveCount(sampleSize, "12-byte SHA-256 truncation should not collide on 10k inputs");
    }
}