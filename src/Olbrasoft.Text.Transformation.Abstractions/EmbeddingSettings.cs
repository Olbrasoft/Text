namespace Olbrasoft.Text.Transformation.Abstractions;

/// <summary>
/// Configuration for embedding services.
/// Only Cohere provider is supported.
/// </summary>
public class EmbeddingSettings
{
    /// <summary>
    /// Which embedding provider to use. Only Cohere is supported.
    /// </summary>
    public EmbeddingProvider Provider { get; set; } = EmbeddingProvider.Cohere;

    /// <summary>
    /// Model name for embedding.
    /// </summary>
    public string Model { get; set; } = "embed-multilingual-v3.0";

    /// <summary>
    /// Vector dimensions for the embedding model.
    /// Cohere embed-multilingual-v3.0: 1024
    /// </summary>
    public int Dimensions { get; set; } = 1024;

    /// <summary>
    /// Cohere-specific settings.
    /// </summary>
    public CohereEmbeddingSettings Cohere { get; set; } = new();

    // === Legacy properties for backward compatibility ===

    /// <summary>
    /// Legacy: Cohere API keys. Use Cohere.ApiKeys instead.
    /// </summary>
    public string[] CohereApiKeys
    {
        get => Cohere.ApiKeys;
        set => Cohere.ApiKeys = value;
    }

    /// <summary>
    /// Legacy: Single Cohere API key.
    /// </summary>
    public string? CohereApiKey { get; set; }

    /// <summary>
    /// Legacy: Cohere model. Use Cohere.Model instead.
    /// </summary>
    public string CohereModel
    {
        get => Cohere.Model;
        set => Cohere.Model = value;
    }

    /// <summary>
    /// Gets all configured Cohere API keys (combines array and legacy single key).
    /// </summary>
    public IReadOnlyList<string> GetCohereApiKeys()
    {
        var keys = new List<string>();

        if (Cohere.ApiKeys.Length > 0)
        {
            keys.AddRange(Cohere.ApiKeys.Where(k => !string.IsNullOrWhiteSpace(k)));
        }

        // Add legacy single key if array is empty
        if (keys.Count == 0 && !string.IsNullOrWhiteSpace(CohereApiKey))
        {
            keys.Add(CohereApiKey);
        }

        return keys;
    }
}

/// <summary>
/// Cohere-specific embedding settings.
/// </summary>
public class CohereEmbeddingSettings
{
    /// <summary>
    /// Cohere embedding model. Default: embed-multilingual-v3.0 (1024 dimensions)
    /// </summary>
    public string Model { get; set; } = "embed-multilingual-v3.0";

    /// <summary>
    /// Vector dimensions. Default: 1024
    /// </summary>
    public int Dimensions { get; set; } = 1024;

    /// <summary>
    /// API keys to rotate through. Store in User Secrets!
    /// </summary>
    public string[] ApiKeys { get; set; } = [];
}
