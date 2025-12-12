namespace Olbrasoft.Text.Transformation.Abstractions;

/// <summary>
/// Settings for summarization behavior.
/// </summary>
public class SummarizationSettings
{
    /// <summary>
    /// Provider to use for summarization. Default: OpenAICompatible
    /// </summary>
    public string Provider { get; set; } = "OpenAICompatible";

    /// <summary>
    /// Model name for summarization.
    /// </summary>
    public string Model { get; set; } = "llama-4-scout-17b-16e-instruct";

    /// <summary>Maximum tokens in summary response.</summary>
    public int MaxTokens { get; set; } = 500;

    /// <summary>Temperature for generation (lower = more deterministic).</summary>
    public double Temperature { get; set; } = 0.3;

    /// <summary>System prompt for summarization.</summary>
    public string SystemPrompt { get; set; } = "You are a helpful assistant that summarizes GitHub issues concisely. Provide a 2-3 sentence summary in English that captures the key points. Start directly with the summary content - do NOT prefix with 'Summary:', 'Summary', or any similar label. Do NOT use <think> tags.";

    /// <summary>
    /// OpenAI-compatible API settings.
    /// </summary>
    public OpenAICompatibleSettings OpenAICompatible { get; set; } = new();
}

/// <summary>
/// Settings for OpenAI-compatible APIs (Cerebras, Groq, etc.)
/// </summary>
public class OpenAICompatibleSettings
{
    /// <summary>
    /// Base URL for the API. Default: Cerebras
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.cerebras.ai/v1";

    /// <summary>
    /// Maximum tokens in response.
    /// </summary>
    public int MaxTokens { get; set; } = 500;

    /// <summary>
    /// Temperature for generation.
    /// </summary>
    public double Temperature { get; set; } = 0.3;

    /// <summary>
    /// API keys to rotate through. Store in User Secrets!
    /// </summary>
    public string[] ApiKeys { get; set; } = [];
}
