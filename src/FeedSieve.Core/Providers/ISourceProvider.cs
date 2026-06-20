namespace FeedSieve.Core.Providers;


/// <summary>
/// Contract every external system integration implements.
/// One implementation per provider type (RSS, HTML watcher, Telegram, ...).
/// Registered as singleton in DI; instances are stateless and thread-safe.
/// </summary>
public interface ISourceProvider
{
    /// <summary>
    /// Stable, lowercase identifier persisted in storage (e.g. "rss", "html-watcher", "telegram").
    /// Must be unique across all registered providers. Used as the JSON polymorphism discriminator.
    /// </summary>
    string ProviderId { get; }

    /// <summary>User-facing name shown in the "Add source" picker.</summary>
    string DisplayName { get; }

    /// <summary>Declarative capability flags. See <see cref="SourceProviderCapabilities"/>.</summary>
    SourceProviderCapabilities Capabilities { get; }

    /// <summary>
    /// The MAUI <c>ContentPage</c> type used to configure a new source of this type.
    /// Resolved through DI by Shell routing; the provider declares the type, not the instance.
    /// </summary>
    Type ConfigurationPageType { get; }

    /// <summary>
    /// The concrete <see cref="SourceConfiguration"/> subtype this provider operates on.
    /// Used by the serializer to wire up polymorphic JSON.
    /// </summary>
    Type ConfigurationType { get; }

    /// <summary>
    /// Fetch new items for the given source.
    /// Implementations MUST honor <paramref name="cancellationToken"/> and SHOULD use
    /// <paramref name="context"/>.LastFingerprint / LastETag to short-circuit when nothing changed.
    /// </summary>
    Task<FetchResult> FetchAsync(
        SourceConfiguration configuration,
        FetchContext context,
        CancellationToken cancellationToken);
}