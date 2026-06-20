namespace FeedSieve.Core.Providers;

/// <summary>
/// Base type for a user-added source. Concrete subtypes carry provider-specific config
/// (e.g. <c>RssSourceConfiguration.FeedUrl</c>, <c>HtmlWatcherSourceConfiguration.CssSelector</c>).
/// </summary>
/// <remarks>
/// Polymorphic JSON is configured at runtime by <see cref="SourceConfigurationSerializer"/>;
/// the base type intentionally has no <c>[JsonDerivedType]</c> attributes so Core does not
/// reference provider projects.
/// </remarks>
public abstract record SourceConfiguration
{
    /// <summary>Globally unique source identifier (GUID-like, set once at creation).</summary>
    public required string Id { get; init; }

    /// <summary>Matches <see cref="ISourceProvider.ProviderId"/> of the owning provider.</summary>
    public required string ProviderId { get; init; }

    /// <summary>User-facing source name (e.g. "Hacker News").</summary>
    public required string DisplayName { get; init; }

    /// <summary>How often to auto-refresh this source. <c>null</c> = manual only.</summary>
    public TimeSpan? RefreshInterval { get; init; }
}