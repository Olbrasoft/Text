namespace Olbrasoft.Text.Translation.DeepL;

/// <summary>
/// Configuration settings for DeepL translation service.
/// </summary>
public class DeepLSettings
{
    /// <summary>
    /// DeepL API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// DeepL API endpoint. Default: https://api-free.deepl.com/v2/
    /// </summary>
    public string Endpoint { get; set; } = "https://api-free.deepl.com/v2/";
}
