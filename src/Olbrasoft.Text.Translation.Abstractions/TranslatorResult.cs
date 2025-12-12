namespace Olbrasoft.Text.Translation;

/// <summary>
/// Result of a translation operation.
/// </summary>
public class TranslatorResult
{
    /// <summary>
    /// Whether the translation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The translated text (null if failed).
    /// </summary>
    public string? Translation { get; set; }

    /// <summary>
    /// Error message (null if successful).
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Provider name (e.g., "DeepL", "Azure").
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Detected source language code (if auto-detection was used).
    /// </summary>
    public string? DetectedSourceLanguage { get; set; }

    /// <summary>
    /// Creates a successful translation result.
    /// </summary>
    public static TranslatorResult Ok(string translation, string provider, string? detectedLang = null) => new()
    {
        Success = true,
        Translation = translation,
        Provider = provider,
        DetectedSourceLanguage = detectedLang
    };

    /// <summary>
    /// Creates a failed translation result.
    /// </summary>
    public static TranslatorResult Fail(string error, string? provider = null) => new()
    {
        Success = false,
        Error = error,
        Provider = provider
    };
}
