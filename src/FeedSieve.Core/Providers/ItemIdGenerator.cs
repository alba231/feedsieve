using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace FeedSieve.Core.Providers;

/// <summary>
/// Computes deterministic, source-scoped item identifiers.
/// Determinism is the foundation of race-safe sync: two devices fetching the same item
/// produce the same id, so Firestore <c>create</c> from the second device 409s harmlessly.
/// </summary>
public static class ItemIdGenerator
{
    private const int IdByteLength = 12; // 24 hex chars; collision-resistant per source
    private const int LengthPrefixSize = sizeof(int);
    private const int StackallocThreshold = 256;

    /// <summary>
    /// Produces a stable 24-char lowercase hex id for an item from
    /// <paramref name="sourceId"/> and the source-supplied <paramref name="canonicalKey"/>.
    /// </summary>
    public static string Compute(string sourceId, string canonicalKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalKey);

        // Length-prefix encoding prevents collisions like ("ab","cde") vs ("abc","de").
        var sourceByteCount = Encoding.UTF8.GetByteCount(sourceId);
        var keyByteCount = Encoding.UTF8.GetByteCount(canonicalKey);
        var totalLength = (LengthPrefixSize * 2) + sourceByteCount + keyByteCount;

        byte[]? rented = null;
        Span<byte> buffer = totalLength <= StackallocThreshold
            ? stackalloc byte[StackallocThreshold]
            : (rented = System.Buffers.ArrayPool<byte>.Shared.Rent(totalLength));

        try
        {
            var working = buffer[..totalLength];
            var offset = 0;

            WriteLengthPrefixed(working, ref offset, sourceId, sourceByteCount);
            WriteLengthPrefixed(working, ref offset, canonicalKey, keyByteCount);

            Span<byte> hash = stackalloc byte[32];
            SHA256.HashData(working, hash);

            return Convert.ToHexStringLower(hash[..IdByteLength]);
        }
        finally
        {
            if (rented is not null)
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static void WriteLengthPrefixed(
        Span<byte> destination,
        ref int offset,
        string value,
        int byteCount)
    {
        BinaryPrimitives.WriteInt32LittleEndian(destination[offset..], byteCount);
        offset += LengthPrefixSize;

        Encoding.UTF8.GetBytes(value, destination[offset..]);
        offset += byteCount;
    }
}