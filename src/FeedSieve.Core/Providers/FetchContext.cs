namespace FeedSieve.Core.Providers;

/// <summary>
/// System-tracked state about prior fetches for a source.
/// All members are nullable: a first-time fetch has no prior state and the provider
/// should not short-circuit.
/// </summary>
/// <remarks>
/// Kept separate from <see cref="SourceConfiguration"/> because configuration is user intent
/// (mutated through the UI) while context is fetch-engine state (mutated after each fetch).
/// Mixing them causes race conditions between user edits and fetch updates.
/// </remarks>
public sealed record FetchContext
{
    /// <summary>Empty context for first-time fetches.</summary>
    public static FetchContext Empty { get; } = new();

    /// <summary>When the last successful fetch completed.</summary>
    public DateTimeOffset? LastFetchedAt { get; init; }

    /// <summary>Content fingerprint from the last fetch (provider-defined hash).</summary>
    public string? LastFingerprint { get; init; }

    /// <summary>HTTP ETag from the last fetch, for conditional GET.</summary>
    public string? LastETag { get; init; }
}