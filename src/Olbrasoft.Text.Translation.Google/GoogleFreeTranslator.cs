using GTranslate.Translators;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Olbrasoft.Text.Translation.Google;

/// <summary>
/// Google Translate free service implementation (unofficial API).
/// </summary>
/// <remarks>
/// This implementation uses the GTranslate library which reverse-engineers
/// Google Translate's free endpoint. No API key is required.
///
/// <para><strong>Important Limitations:</strong></para>
/// <list type="bullet">
/// <item>Unofficial API - may change without notice</item>
/// <item>Rate limiting: ~100 requests/hour per IP</item>
/// <item>Not suitable for high-volume production use</item>
/// <item>Best for personal/low-volume use cases</item>
/// </list>
/// </remarks>
public class GoogleFreeTranslator : ITranslator
{
    private readonly GoogleTranslator _translator;
    private readonly GoogleFreeTranslatorSettings _settings;
    private readonly ILogger<GoogleFreeTranslator> _logger;

    public GoogleFreeTranslator(
        IOptions<GoogleFreeTranslatorSettings> settings,
        ILogger<GoogleFreeTranslator> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _translator = new GoogleTranslator();
    }

    /// <summary>
    /// Translates text using Google Translate free API.
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
            return TranslatorResult.Fail("Text cannot be empty", "Google");

        try
        {
            _logger.LogDebug("Google Translate: {TargetLang}, text length: {Length}, source: {Source}",
                targetLanguage, text.Length, sourceLanguage ?? "auto");

            // Use timeout from settings
            // Note: GTranslate doesn't accept CancellationToken, so we use Task.WaitAsync for timeout
            var translateTask = _translator.TranslateAsync(
                text,
                targetLanguage,
                sourceLanguage);

            var result = await translateTask.WaitAsync(
                TimeSpan.FromSeconds(_settings.TimeoutSeconds),
                cancellationToken);

            if (result == null || string.IsNullOrWhiteSpace(result.Translation))
                return TranslatorResult.Fail("Empty response from Google Translate", "Google");

            _logger.LogDebug("Google translation successful, detected: {Lang}",
                result.SourceLanguage?.Name ?? "unknown");

            return TranslatorResult.Ok(
                result.Translation,
                "Google",
                result.SourceLanguage?.ISO6391);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Google Translate request cancelled by user");
            return TranslatorResult.Fail("Translation cancelled", "Google");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Google Translate request timed out after {Timeout}s", _settings.TimeoutSeconds);
            return TranslatorResult.Fail($"Translation timed out after {_settings.TimeoutSeconds}s", "Google");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Google Translate HTTP error");
            return TranslatorResult.Fail($"Network error: {ex.Message}", "Google");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google Translate translation failed");
            return TranslatorResult.Fail($"Translation failed: {ex.Message}", "Google");
        }
    }
}
