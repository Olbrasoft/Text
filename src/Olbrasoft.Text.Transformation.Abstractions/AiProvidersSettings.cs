namespace Olbrasoft.Text.Transformation.Abstractions;

/// <summary>
/// Settings for AI providers used in summarization and translation.
/// </summary>
public class AiProvidersSettings
{
    public AiProviderConfig Cerebras { get; set; } = new();
    public AiProviderConfig Groq { get; set; } = new();
    public CohereProviderConfig Cohere { get; set; } = new();
}

/// <summary>
/// Configuration for Cohere provider (uses different API format).
/// </summary>
public class CohereProviderConfig
{
    /// <summary>API endpoint URL.</summary>
    public string Endpoint { get; set; } = "https://api.cohere.com/v2/";

    /// <summary>Models to use for translation, in priority order.</summary>
    public string[] TranslationModels { get; set; } = ["command-a-translate-08-2025", "c4ai-aya-expanse-32b"];

    /// <summary>API keys to rotate through.</summary>
    public string[] Keys { get; set; } = [];
}

/// <summary>
/// Configuration for a single AI provider.
/// </summary>
public class AiProviderConfig
{
    /// <summary>API endpoint URL.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Models to use, in priority order.</summary>
    public string[] Models { get; set; } = [];

    /// <summary>API keys to rotate through. Store in User Secrets!</summary>
    public string[] Keys { get; set; } = [];
}
