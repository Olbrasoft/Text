namespace Olbrasoft.Text.Translation.Bing;

/// <summary>
/// Settings for Bing Translator free service.
/// </summary>
public class BingFreeTranslatorSettings
{
    /// <summary>
    /// Timeout in seconds for Bing Translate requests. Default: 10 seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Optional custom user agent. If null, uses GTranslate's default.
    /// </summary>
    public string? UserAgent { get; set; }
}
