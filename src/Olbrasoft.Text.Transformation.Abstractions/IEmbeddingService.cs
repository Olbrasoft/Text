namespace Olbrasoft.Text.Transformation.Abstractions;

/// <summary>
/// Specifies the type of input for embedding generation.
/// Some providers (like Cohere) optimize embeddings based on input type.
/// </summary>
public enum EmbeddingInputType
{
    /// <summary>
    /// Use for documents being indexed/stored (e.g., issue content).
    /// </summary>
    Document,

    /// <summary>
    /// Use for search queries entered by users.
    /// </summary>
    Query
}

/// <summary>
/// Generic embedding service interface following Interface Segregation Principle (ISP).
/// Can be implemented by Ollama, OpenAI, Azure OpenAI, Cohere, or other embedding providers.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generates a vector embedding for the given text.
    /// </summary>
    /// <param name="text">Text to embed.</param>
    /// <param name="inputType">Type of input - Document for indexing, Query for searching.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<float[]?> GenerateEmbeddingAsync(string text, EmbeddingInputType inputType = EmbeddingInputType.Document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the embedding service is available and responding.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Indicates whether the service is properly configured.
    /// </summary>
    bool IsConfigured { get; }
}
