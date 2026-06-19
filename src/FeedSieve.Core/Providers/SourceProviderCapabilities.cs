namespace FeedSieve.Core.Providers;

/// <summary>
/// Declarative feature flags describing what a provider supports.
/// Consumers can branch on capabilities without type-checking provider implementations.
/// </summary>
[Flags]
public enum SourceProviderCapabilities
{
    None = 0,

    /// <summary>Provider can be polled on a schedule (most pull-based sources).</summary>
    SupportsScheduledFetch = 1 << 0,

    /// <summary>Provider can ingest pushed events (e.g., Discord webhooks, Telegram bot updates).</summary>
    SupportsPushIngest = 1 << 1,

    /// <summary>Provider requires user-supplied credentials beyond a public URL.</summary>
    RequiresAuth = 1 << 2,

    /// <summary>Provider can return full item body, not just metadata.</summary>
    SupportsBodyFetch = 1 << 3,
}