using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace FeedSieve.Core.Providers;

/// <summary>
/// Serializes <see cref="SourceConfiguration"/> polymorphically using runtime type registration
/// from <see cref="ProviderRegistry"/>. The discriminator is <c>$type</c> and matches
/// <see cref="ISourceProvider.ProviderId"/>.
/// </summary>
/// <remarks>
/// Runtime registration (vs. <c>[JsonDerivedType]</c> attributes) keeps Core decoupled from
/// provider projects. Cost: reflection. For iOS AOT, derived types must be preserved via
/// <c>[DynamicallyAccessedMembers]</c> or trimming roots.
/// </remarks>
public sealed class SourceConfigurationSerializer
{
    public const string TypeDiscriminatorPropertyName = "$type";

    private readonly JsonSerializerOptions _options;

    public SourceConfigurationSerializer(ProviderRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        var derivedTypes = registry.All
            .Select(p => new JsonDerivedType(p.ConfigurationType, p.ProviderId))
            .ToArray();

        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(typeInfo => ConfigurePolymorphism(typeInfo, derivedTypes));

        _options = new JsonSerializerOptions
        {
            TypeInfoResolver = resolver,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }

    /// <summary>Serializes a configuration to JSON with the polymorphic discriminator.</summary>
    public string Serialize(SourceConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return JsonSerializer.Serialize<SourceConfiguration>(configuration, _options);
    }

    /// <summary>
    /// Deserializes JSON back to the concrete configuration type indicated by the discriminator.
    /// </summary>
    /// <exception cref="JsonException">
    /// Thrown when the JSON is malformed, the discriminator is missing, or the discriminator
    /// references a provider that is not registered.
    /// </exception>
    public SourceConfiguration Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        return JsonSerializer.Deserialize<SourceConfiguration>(json, _options)
               ?? throw new JsonException("Deserialization returned null.");
    }

    private static void ConfigurePolymorphism(
        JsonTypeInfo typeInfo,
        IReadOnlyList<JsonDerivedType> derivedTypes)
    {
        if (typeInfo.Type != typeof(SourceConfiguration)) return;

        typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
        {
            TypeDiscriminatorPropertyName = TypeDiscriminatorPropertyName,
            IgnoreUnrecognizedTypeDiscriminators = false,
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
        };

        foreach (var derived in derivedTypes)
        {
            typeInfo.PolymorphismOptions.DerivedTypes.Add(derived);
        }
    }
}