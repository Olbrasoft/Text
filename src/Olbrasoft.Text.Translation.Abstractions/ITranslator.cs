namespace Olbrasoft.Text.Translation;

/// <summary>
/// Interface for dedicated translation services (DeepL, Azure Translator).
/// </summary>
public interface ITranslator
{
    /// <summary>
    /// Translates text to the target language.
    /// </summary>
    /// <param name="text">Text to translate.</param>
    /// <param name="targetLanguage">Target language code (e.g., "cs", "de", "en").</param>
    /// <param name="sourceLanguage">Source language code. Null for auto-detection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Translation result.</returns>
    Task<TranslatorResult> TranslateAsync(
        string text,
        string targetLanguage,
        string? sourceLanguage = null,
        CancellationToken cancellationToken = default);
}
