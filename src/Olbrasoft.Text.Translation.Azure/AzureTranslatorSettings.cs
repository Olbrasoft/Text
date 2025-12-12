namespace Olbrasoft.Text.Translation.Azure;

/// <summary>
/// Configuration settings for Azure Translator service.
/// </summary>
public class AzureTranslatorSettings
{
    /// <summary>
    /// Azure Translator API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Azure region (e.g., "westeurope").
    /// </summary>
    public string Region { get; set; } = "westeurope";

    /// <summary>
    /// Azure Translator API endpoint.
    /// </summary>
    public string Endpoint { get; set; } = "https://api.cognitive.microsofttranslator.com/";
}
