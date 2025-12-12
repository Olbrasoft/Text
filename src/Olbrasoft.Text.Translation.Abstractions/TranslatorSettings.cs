namespace Olbrasoft.Text.Translation;

/// <summary>
/// Base settings for translation providers.
/// </summary>
public class TranslatorSettings
{
    /// <summary>
    /// API key for the translation service.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// API endpoint URL.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Region (required for Azure Translator).
    /// </summary>
    public string? Region { get; set; }
}
