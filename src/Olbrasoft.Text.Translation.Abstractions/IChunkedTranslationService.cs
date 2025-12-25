namespace Olbrasoft.Text.Translation;

/// <summary>
/// Translation service that automatically handles text chunking for long texts
/// based on provider-specific character limits.
/// </summary>
public interface IChunkedTranslationService
{
    /// <summary>
    /// Translates text using the provided translator, automatically chunking if needed.
    /// If the text exceeds the translator's MaxRequestCharacters limit, it will be
    /// split into smaller chunks, translated separately, and reassembled.
    /// </summary>
    /// <param name="translator">The translator to use for translation.</param>
    /// <param name="text">Text to translate (can be longer than translator's limit).</param>
    /// <param name="targetLanguage">Target language code (e.g., "cs", "de", "en").</param>
    /// <param name="sourceLanguage">Source language code. Null for auto-detection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Translation result with complete translated text.</returns>
    Task<TranslatorResult> TranslateAsync(
        ITranslator translator,
        string text,
        string targetLanguage,
        string? sourceLanguage = null,
        CancellationToken cancellationToken = default);
}
