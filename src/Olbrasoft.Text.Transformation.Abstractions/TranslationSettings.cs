namespace Olbrasoft.Text.Transformation.Abstractions;

/// <summary>
/// Settings for translation behavior.
/// </summary>
public class TranslationSettings
{
    /// <summary>
    /// Provider to use for translation. Default: Cohere
    /// </summary>
    public string Provider { get; set; } = "Cohere";

    /// <summary>
    /// Model name for translation.
    /// </summary>
    public string Model { get; set; } = "command-a-03-2025";

    /// <summary>Maximum tokens in translation response.</summary>
    public int MaxTokens { get; set; } = 300;

    /// <summary>Temperature for generation (lower = more deterministic).</summary>
    public double Temperature { get; set; } = 0.2;

    /// <summary>Target language for translation.</summary>
    public string TargetLanguage { get; set; } = "Czech";

    /// <summary>System prompt for translation (for OpenAI-compatible providers).</summary>
    public string SystemPrompt { get; set; } = "Translate the following text to Czech. Preserve the meaning and tone. Output only the translation, nothing else.";

    /// <summary>
    /// Cohere-specific translation settings.
    /// </summary>
    public CohereTranslationSettings Cohere { get; set; } = new();

    /// <summary>
    /// Fallback provider configuration when primary fails.
    /// </summary>
    public TranslationFallbackSettings? Fallback { get; set; }
}

/// <summary>
/// Cohere-specific translation settings.
/// </summary>
public class CohereTranslationSettings
{
    /// <summary>
    /// Cohere API endpoint.
    /// </summary>
    public string Endpoint { get; set; } = "https://api.cohere.com/v2/";

    /// <summary>
    /// API keys to rotate through. Store in User Secrets!
    /// </summary>
    public string[] ApiKeys { get; set; } = [];
}

/// <summary>
/// Fallback provider settings for translation.
/// </summary>
public class TranslationFallbackSettings
{
    /// <summary>
    /// Fallback provider name.
    /// </summary>
    public string Provider { get; set; } = "OpenAICompatible";

    /// <summary>
    /// Fallback model name.
    /// </summary>
    public string Model { get; set; } = "llama-3.3-70b-versatile";

    /// <summary>
    /// OpenAI-compatible API settings for fallback.
    /// </summary>
    public OpenAICompatibleSettings OpenAICompatible { get; set; } = new()
    {
        BaseUrl = "https://api.groq.com/openai/v1"
    };
}
