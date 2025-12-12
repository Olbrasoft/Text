using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Olbrasoft.Text.Translation.Azure;

/// <summary>
/// Azure Translator service implementation.
/// </summary>
public class AzureTranslator : ITranslator
{
    private readonly HttpClient _httpClient;
    private readonly AzureTranslatorSettings _settings;
    private readonly ILogger<AzureTranslator> _logger;

    public AzureTranslator(
        HttpClient httpClient,
        IOptions<AzureTranslatorSettings> settings,
        ILogger<AzureTranslator> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.Endpoint);
        _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _settings.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Region", _settings.Region);
    }

    public async Task<TranslatorResult> TranslateAsync(
        string text,
        string targetLanguage,
        string? sourceLanguage = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return TranslatorResult.Fail("Text cannot be empty", "Azure");

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            return TranslatorResult.Fail("Azure Translator API key not configured", "Azure");

        try
        {
            // Build query string
            var query = $"translate?api-version=3.0&to={targetLanguage.ToLowerInvariant()}";
            if (!string.IsNullOrEmpty(sourceLanguage))
                query += $"&from={sourceLanguage.ToLowerInvariant()}";

            var requestBody = new[] { new AzureTranslateRequest { Text = text } };

            _logger.LogDebug("Azure translate: {TargetLang}, text length: {Length}",
                targetLanguage, text.Length);

            var response = await _httpClient.PostAsJsonAsync(query, requestBody, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Azure Translator API error: {StatusCode} - {Content}",
                    response.StatusCode, errorContent);
                return TranslatorResult.Fail($"Azure API error: {response.StatusCode}", "Azure");
            }

            var result = await response.Content.ReadFromJsonAsync<List<AzureTranslateResponse>>(cancellationToken);
            if (result is not { Count: > 0 } || result[0].Translations is not { Count: > 0 })
                return TranslatorResult.Fail("Empty response from Azure Translator", "Azure");

            var translation = result[0];
            var detectedLang = translation.DetectedLanguage?.Language;

            _logger.LogDebug("Azure translation successful, detected: {Lang}", detectedLang);

            return TranslatorResult.Ok(
                translation.Translations[0].Text,
                "Azure",
                detectedLang);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Azure Translator HTTP error");
            return TranslatorResult.Fail($"Network error: {ex.Message}", "Azure");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure translation failed");
            return TranslatorResult.Fail($"Translation failed: {ex.Message}", "Azure");
        }
    }

    private class AzureTranslateRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private class AzureTranslateResponse
    {
        [JsonPropertyName("detectedLanguage")]
        public DetectedLanguageInfo? DetectedLanguage { get; set; }

        [JsonPropertyName("translations")]
        public List<TranslationInfo> Translations { get; set; } = [];
    }

    private class DetectedLanguageInfo
    {
        [JsonPropertyName("language")]
        public string Language { get; set; } = string.Empty;

        [JsonPropertyName("score")]
        public double Score { get; set; }
    }

    private class TranslationInfo
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("to")]
        public string To { get; set; } = string.Empty;
    }
}
