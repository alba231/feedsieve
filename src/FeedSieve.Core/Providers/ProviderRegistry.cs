using System.Collections.Frozen;

namespace FeedSieve.Core.Providers;

/// <summary>
/// Read-only catalog of registered providers, indexed by <see cref="ISourceProvider.ProviderId"/>.
/// Validates uniqueness at construction so duplicates fail fast at startup.
/// </summary>
public sealed class ProviderRegistry
{
    private readonly FrozenDictionary<string, ISourceProvider> _byProviderId;
    private readonly FrozenDictionary<Type, ISourceProvider> _byConfigurationType;

    public ProviderRegistry(IEnumerable<ISourceProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        var materialized = providers.ToList();

        EnsureUniqueProviderIds(materialized);
        EnsureUniqueConfigurationTypes(materialized);

        _byProviderId = materialized.ToFrozenDictionary(
            p => p.ProviderId,
            StringComparer.OrdinalIgnoreCase);

        _byConfigurationType = materialized.ToFrozenDictionary(p => p.ConfigurationType);
    }

    /// <summary>All registered providers, in registration order is not guaranteed.</summary>
    public IReadOnlyCollection<ISourceProvider> All => _byProviderId.Values;

    /// <summary>Lookup by provider id. Case-insensitive.</summary>
    /// <exception cref="KeyNotFoundException">No provider with that id is registered.</exception>
    public ISourceProvider Get(string providerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        return _byProviderId.TryGetValue(providerId, out var provider)
            ? provider
            : throw new KeyNotFoundException($"No provider registered with id '{providerId}'.");
    }

    /// <summary>Non-throwing lookup by provider id. Case-insensitive.</summary>
    public bool TryGet(string providerId, out ISourceProvider? provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        return _byProviderId.TryGetValue(providerId, out provider);
    }

    /// <summary>Lookup by the concrete configuration <see cref="Type"/>.</summary>
    /// <exception cref="KeyNotFoundException">No provider operates on that configuration type.</exception>
    public ISourceProvider GetForConfiguration(Type configurationType)
    {
        ArgumentNullException.ThrowIfNull(configurationType);

        return _byConfigurationType.TryGetValue(configurationType, out var provider)
            ? provider
            : throw new KeyNotFoundException(
                $"No provider registered for configuration type '{configurationType.FullName}'.");
    }

    private static void EnsureUniqueProviderIds(IReadOnlyList<ISourceProvider> providers)
    {
        var duplicate = providers
            .GroupBy(p => p.ProviderId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Duplicate provider id '{duplicate.Key}' registered ({duplicate.Count()} times). " +
                $"Provider ids must be unique (case-insensitive).");
        }
    }

    private static void EnsureUniqueConfigurationTypes(IReadOnlyList<ISourceProvider> providers)
    {
        var duplicate = providers
            .GroupBy(p => p.ConfigurationType)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Multiple providers declare configuration type '{duplicate.Key.FullName}'. " +
                $"Each provider must own a distinct configuration type.");
        }
    }
}