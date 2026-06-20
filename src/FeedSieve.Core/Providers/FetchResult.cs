namespace FeedSieve.Core.Providers;

/// <summary>
/// Output of a single <see cref="ISourceProvider.FetchAsync"/> call.
/// Envelope (not a bare list) because <see cref="ContentFingerprint"/> and <see cref="ETag"/>
/// are facts about the fetch, not about individual items.
/// </summary>
public sealed record FetchResult
{
    /// <summary>Indicates the source returned a "not modified" signal; <see cref="Items"/> may be empty.</summary>
    public static FetchResult NotModified(string fingerprint, string? etag = null) =>
        new()
        {
            Items = [],
            ContentFingerprint = fingerprint,
            ETag = etag,
            IsNotModified = true,
        };

    /// <summary>Newly fetched items (post-deduplication is the engine's responsibility).</summary>
    public required IReadOnlyList<RawItem> Items { get; init; }

    /// <summary>
    /// Provider-defined hash of the fetched content, used for change detection across fetches.
    /// REQUIRED so the engine can always update <see cref="FetchContext.LastFingerprint"/>;
    /// providers must compute a fingerprint even on empty fetches.
    /// </summary>
    public required string ContentFingerprint { get; init; }

    /// <summary>HTTP ETag returned by the source, if applicable.</summary>
    public string? ETag { get; init; }

    /// <summary>
    /// When the fetch completed. Defaults to <see cref="DateTimeOffset.UtcNow"/> at
    /// construction time so providers can omit it for the common case.
    /// </summary>
    public DateTimeOffset FetchedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Set when the source replied "nothing changed" (e.g., HTTP 304). When <c>true</c>,
    /// <see cref="Items"/> should be empty and the engine skips upsert.
    /// </summary>
    public bool IsNotModified { get; init; }
}