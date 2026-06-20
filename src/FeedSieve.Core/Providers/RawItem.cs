namespace FeedSieve.Core.Providers;

/// <summary>
/// A provider's view of a single item — content without app-level state.
/// </summary>
/// <remarks>
/// Deliberately named "Raw" to distinguish from the persisted <c>Item</c> entity, which adds
/// filter results, read/pinned state, and other concerns the provider knows nothing about.
/// </remarks>
public sealed record RawItem
{
    /// <summary>
    /// Source-stable identifier for the item (e.g. RSS <c>&lt;guid&gt;</c>, Telegram message id).
    /// Combined with the source id via <c>ItemIdGenerator</c> to produce the global item id.
    /// </summary>
    public required string CanonicalKey { get; init; }

    /// <summary>Item title — required even for sources without a natural title (provider synthesizes).</summary>
    public required string Title { get; init; }

    /// <summary>Permalink to the item, if applicable.</summary>
    public Uri? Url { get; init; }

    /// <summary>Short summary; bodies are fetched lazily per device, not via this record.</summary>
    public string? Summary { get; init; }

    /// <summary>Publication time as reported by the source.</summary>
    public DateTimeOffset? PublishedAt { get; init; }

    /// <summary>Source-supplied tags/categories. Defaults to empty — most providers don't surface them.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}