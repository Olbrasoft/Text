using GTranslate.Translators;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Olbrasoft.Text.Translation.Bing;

/// <summary>
/// Bing Translator free service implementation (unofficial API via web scraping).
/// </summary>
/// <remarks>
/// This implementation uses the GTranslate library which scrapes Bing Translator's
/// free web endpoint. No API key is required.
///
/// <para><strong>Important Limitations:</strong></para>
/// <list type="bullet">
/// <item>Unofficial API - may change without notice</item>
/// <item>Web scraping based - extracts credentials from bing.com/translator</item>
/// <item>Rate limiting may apply (exact limits unknown)</item>
/// <item>Best for personal/low-volume use cases</item>
/// </list>
/// </remarks>
public class BingFreeTranslator : ITranslator
{
    private readonly BingTranslator _translator;
    private readonly BingFreeTranslatorSettings _settings;
    private readonly ILogger<BingFreeTranslator> _logger;

    /// <summary>
    /// Maximum request size for Bing Translator (unofficial API).
    /// Source: GTranslate library hardcoded limit.
    /// </summary>
    public int MaxRequestCharacters => 1000;

    public BingFreeTranslator(
        IOptions<BingFreeTranslatorSettings> settings,
        ILogger<BingFreeTranslator> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _translator = new BingTranslator();
    }

    /// <summary>
    /// Translates text using Bing Translator free API.
    /// </summary>
    /// <param name="text">Text to translate.</param>
    /// <param name="targetLanguage">Target language code (e.g., "cs", "de", "en").</param>
    /// <param name="sourceLanguage">Source language code. Null for auto-detection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Translation result.</returns>
    public async Task<TranslatorResult> TranslateAsync(
        string text,
        string targetLanguage,
        string? sourceLanguage = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return TranslatorResult.Fail("Text cannot be empty", "Bing");

        try
        {
            _logger.LogDebug("Bing Translate: {TargetLang}, text length: {Length}, source: {Source}",
                targetLanguage, text.Length, sourceLanguage ?? "auto");

            // Use timeout via Task.WaitAsync
            var translateTask = _translator.TranslateAsync(
                text,
                targetLanguage,
                sourceLanguage);

            var result = await translateTask.WaitAsync(
                TimeSpan.FromSeconds(_settings.TimeoutSeconds),
                cancellationToken);

            if (result == null || string.IsNullOrWhiteSpace(result.Translation))
                return TranslatorResult.Fail("Empty response from Bing Translator", "Bing");

            _logger.LogDebug("Bing translation successful, detected: {Lang}",
                result.SourceLanguage?.Name ?? "unknown");

            return TranslatorResult.Ok(
                result.Translation,
                "Bing",
                result.SourceLanguage?.ISO6391);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Bing Translate request cancelled by user");
            return TranslatorResult.Fail("Translation cancelled", "Bing");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Bing Translate request timed out after {Timeout}s", _settings.TimeoutSeconds);
            return TranslatorResult.Fail($"Translation timed out after {_settings.TimeoutSeconds}s", "Bing");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Bing Translate HTTP error");
            return TranslatorResult.Fail($"Network error: {ex.Message}", "Bing");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bing Translate translation failed");
            return TranslatorResult.Fail($"Translation failed: {ex.Message}", "Bing");
        }
    }
}
