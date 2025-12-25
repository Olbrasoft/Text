namespace Olbrasoft.Text.Translation.Google;

/// <summary>
/// Configuration settings for Google Translate free service.
/// </summary>
/// <remarks>
/// Google Translate free service does not require an API key.
/// This class is provided for future extensibility (e.g., rate limiting configuration).
/// </remarks>
public class GoogleFreeTranslatorSettings
{
    /// <summary>
    /// Optional timeout for translation requests in seconds. Default: 10 seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Optional user agent string for HTTP requests.
    /// </summary>
    public string? UserAgent { get; set; }
}
