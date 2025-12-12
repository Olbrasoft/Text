using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Olbrasoft.Text.Translation.DeepL;

/// <summary>
/// DeepL translation service implementation.
/// </summary>
public class DeepLTranslator : ITranslator
{
    private readonly HttpClient _httpClient;
    private readonly DeepLSettings _settings;
    private readonly ILogger<DeepLTranslator> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public DeepLTranslator(
        HttpClient httpClient,
        IOptions<DeepLSettings> settings,
        ILogger<DeepLTranslator> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.Endpoint);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"DeepL-Auth-Key {_settings.ApiKey}");
    }

    public async Task<TranslatorResult> TranslateAsync(
        string text,
        string targetLanguage,
        string? sourceLanguage = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return TranslatorResult.Fail("Text cannot be empty", "DeepL");

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            return TranslatorResult.Fail("DeepL API key not configured", "DeepL");

        try
        {
            var request = new DeepLRequest
            {
                Text = [text],
                TargetLang = targetLanguage.ToUpperInvariant(),
                SourceLang = sourceLanguage?.ToUpperInvariant()
            };

            _logger.LogDebug("DeepL translate: {TargetLang}, text length: {Length}",
                request.TargetLang, text.Length);

            var response = await _httpClient.PostAsJsonAsync("translate", request, JsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("DeepL API error: {StatusCode} - {Content}",
                    response.StatusCode, errorContent);
                return TranslatorResult.Fail($"DeepL API error: {response.StatusCode}", "DeepL");
            }

            var result = await response.Content.ReadFromJsonAsync<DeepLResponse>(JsonOptions, cancellationToken);
            if (result?.Translations is not { Count: > 0 })
                return TranslatorResult.Fail("Empty response from DeepL", "DeepL");

            var translation = result.Translations[0];
            _logger.LogDebug("DeepL translation successful, detected: {Lang}",
                translation.DetectedSourceLanguage);

            return TranslatorResult.Ok(
                translation.Text,
                "DeepL",
                translation.DetectedSourceLanguage?.ToLowerInvariant());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "DeepL HTTP error");
            return TranslatorResult.Fail($"Network error: {ex.Message}", "DeepL");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeepL translation failed");
            return TranslatorResult.Fail($"Translation failed: {ex.Message}", "DeepL");
        }
    }

    private class DeepLRequest
    {
        [JsonPropertyName("text")]
        public string[] Text { get; set; } = [];

        [JsonPropertyName("target_lang")]
        public string TargetLang { get; set; } = string.Empty;

        [JsonPropertyName("source_lang")]
        public string? SourceLang { get; set; }
    }

    private class DeepLResponse
    {
        [JsonPropertyName("translations")]
        public List<DeepLTranslation> Translations { get; set; } = [];
    }

    private class DeepLTranslation
    {
        [JsonPropertyName("detected_source_language")]
        public string? DetectedSourceLanguage { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
}
