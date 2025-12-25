using Microsoft.Extensions.Logging;

namespace Olbrasoft.Text.Translation;

/// <summary>
/// Translation service that automatically handles text chunking for long texts
/// based on provider-specific character limits.
/// </summary>
public class ChunkedTranslationService : IChunkedTranslationService
{
    private readonly ITextChunker _textChunker;
    private readonly ILogger<ChunkedTranslationService> _logger;

    public ChunkedTranslationService(
        ITextChunker textChunker,
        ILogger<ChunkedTranslationService> logger)
    {
        _textChunker = textChunker;
        _logger = logger;
    }

    /// <summary>
    /// Translates text using the provided translator, automatically chunking if needed.
    /// </summary>
    public async Task<TranslatorResult> TranslateAsync(
        ITranslator translator,
        string text,
        string targetLanguage,
        string? sourceLanguage = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return TranslatorResult.Fail("Text cannot be empty");

        if (translator == null)
            return TranslatorResult.Fail("Translator cannot be null");

        // Check if text needs chunking
        if (text.Length <= translator.MaxRequestCharacters)
        {
            _logger.LogDebug("Text length {Length} within limit {Limit}, translating directly",
                text.Length, translator.MaxRequestCharacters);

            return await translator.TranslateAsync(text, targetLanguage, sourceLanguage, cancellationToken);
        }

        // Text exceeds limit, chunk it
        _logger.LogInformation(
            "Text length {Length} exceeds limit {Limit}, chunking into smaller pieces",
            text.Length, translator.MaxRequestCharacters);

        var chunks = _textChunker.ChunkText(text, translator.MaxRequestCharacters);
        _logger.LogInformation("Split text into {Count} chunks", chunks.Count);

        var translatedChunks = new List<string>();
        var detectedLanguage = sourceLanguage;
        string? providerName = null;

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            _logger.LogDebug("Translating chunk {Index}/{Total}, length: {Length}",
                i + 1, chunks.Count, chunk.Length);

            var result = await translator.TranslateAsync(
                chunk,
                targetLanguage,
                sourceLanguage,
                cancellationToken);

            if (!result.Success)
            {
                _logger.LogError(
                    "Translation failed for chunk {Index}/{Total}: {Error}",
                    i + 1, chunks.Count, result.Error);

                // Return partial failure with what we translated so far
                if (translatedChunks.Count > 0)
                {
                    var partialTranslation = _textChunker.JoinChunks(translatedChunks);
                    return TranslatorResult.Fail(
                        $"Partial translation failure at chunk {i + 1}/{chunks.Count}: {result.Error}. " +
                        $"Translated {translatedChunks.Count} chunks successfully.",
                        result.Provider);
                }

                return result; // First chunk failed, return original error
            }

            translatedChunks.Add(result.Translation!);

            // Capture provider and detected language from first chunk
            if (i == 0)
            {
                providerName = result.Provider;
                if (result.DetectedSourceLanguage != null)
                {
                    detectedLanguage = result.DetectedSourceLanguage;
                }
            }

            _logger.LogDebug("Chunk {Index}/{Total} translated successfully",
                i + 1, chunks.Count);
        }

        // Join all translated chunks
        var fullTranslation = _textChunker.JoinChunks(translatedChunks);

        _logger.LogInformation(
            "Successfully translated all {Count} chunks, final length: {Length}",
            chunks.Count, fullTranslation.Length);

        return TranslatorResult.Ok(
            fullTranslation,
            providerName ?? "Unknown",
            detectedLanguage);
    }
}
