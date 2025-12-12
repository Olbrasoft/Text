namespace Olbrasoft.Text.Transformation.Abstractions;

/// <summary>
/// Service for AI-powered translation of text content.
/// Uses different providers than summarization to spread API usage.
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Translates the given text to Czech.
    /// </summary>
    /// <param name="text">Text to translate (typically an English summary).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Translation result with status information.</returns>
    Task<TranslationResult> TranslateToCzechAsync(string text, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a translation request.
/// </summary>
public class TranslationResult
{
    /// <summary>Whether translation was successful.</summary>
    public bool Success { get; set; }

    /// <summary>The translated text, or null if failed.</summary>
    public string? Translation { get; set; }

    /// <summary>Error message if translation failed.</summary>
    public string? Error { get; set; }

    /// <summary>Which provider was used (for logging).</summary>
    public string? Provider { get; set; }

    /// <summary>Which model was used (for logging).</summary>
    public string? Model { get; set; }

    /// <summary>Creates a successful result.</summary>
    public static TranslationResult Ok(string translation, string provider, string model) =>
        new() { Success = true, Translation = translation, Provider = provider, Model = model };

    /// <summary>Creates a failed result.</summary>
    public static TranslationResult Fail(string error) =>
        new() { Success = false, Error = error };
}
