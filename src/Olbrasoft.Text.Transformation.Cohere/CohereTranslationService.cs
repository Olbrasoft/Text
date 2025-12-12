using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Olbrasoft.Text.Transformation.Abstractions;

namespace Olbrasoft.Text.Transformation.Cohere;

/// <summary>
/// Cohere-based translation service using specialized translation models.
/// </summary>
public class CohereTranslationService : ITranslationService
{
    private readonly HttpClient _httpClient;
    private readonly AiProvidersSettings _providers;
    private readonly ILogger<CohereTranslationService> _logger;

    // Thread-safe key rotation
    private static int _keyIndex;

    public CohereTranslationService(
        HttpClient httpClient,
        IOptions<AiProvidersSettings> providers,
        ILogger<CohereTranslationService> logger)
    {
        _httpClient = httpClient;
        _providers = providers.Value;
        _logger = logger;
    }

    public async Task<TranslationResult> TranslateToCzechAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return TranslationResult.Fail("No text to translate");
        }

        var cohereKeys = _providers.Cohere.Keys.Where(k => !string.IsNullOrEmpty(k)).ToList();
        if (cohereKeys.Count == 0)
        {
            _logger.LogDebug("No Cohere API keys configured");
            return TranslationResult.Fail("No Cohere keys");
        }

        var models = _providers.Cohere.TranslationModels;
        if (models.Length == 0)
        {
            models = ["command-a-translate-08-2025", "c4ai-aya-expanse-32b"];
        }

        // Try each key/model combination
        foreach (var model in models)
        {
            var keyIndex = Interlocked.Increment(ref _keyIndex) % cohereKeys.Count;
            var apiKey = cohereKeys[keyIndex];

            try
            {
                var result = await CallCohereAsync(apiKey, model, text, cancellationToken);
                if (result.Success)
                {
                    return result;
                }
                _logger.LogWarning("Cohere translation failed with {Model}: {Error}", model, result.Error);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception during Cohere translation with {Model}", model);
            }
        }

        return TranslationResult.Fail("All Cohere attempts failed");
    }

    private async Task<TranslationResult> CallCohereAsync(
        string apiKey,
        string model,
        string text,
        CancellationToken cancellationToken)
    {
        // Cohere v2 chat API format
        var request = new CohereRequest
        {
            Model = model,
            Messages =
            [
                new CohereMessage
                {
                    Role = "user",
                    Content = $"Translate the following text to Czech. Output only the translation, nothing else.\n\n{text}"
                }
            ]
        };

        var json = JsonSerializer.Serialize(request, CohereTranslationJsonContext.Default.CohereRequest);
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _providers.Cohere.Endpoint + "chat");
        httpRequest.Content = httpContent;
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        _logger.LogDebug("Trying Cohere translation with {Model}", model);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return TranslationResult.Fail($"HTTP {(int)response.StatusCode}: {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var cohereResponse = JsonSerializer.Deserialize(responseBody, CohereTranslationJsonContext.Default.CohereResponse);

        var translation = cohereResponse?.Message?.Content?.FirstOrDefault()?.Text;

        if (string.IsNullOrEmpty(translation))
        {
            return TranslationResult.Fail("Empty response from Cohere");
        }

        _logger.LogInformation("Translation successful with Cohere/{Model}", model);
        return TranslationResult.Ok(translation, "Cohere", model);
    }
}

// Cohere API models
internal class CohereRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public CohereMessage[] Messages { get; set; } = [];
}

internal class CohereMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

internal class CohereResponse
{
    [JsonPropertyName("message")]
    public CohereResponseMessage? Message { get; set; }
}

internal class CohereResponseMessage
{
    [JsonPropertyName("content")]
    public CohereContentItem[]? Content { get; set; }
}

internal class CohereContentItem
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

[JsonSerializable(typeof(CohereRequest))]
[JsonSerializable(typeof(CohereResponse))]
internal partial class CohereTranslationJsonContext : JsonSerializerContext
{
}
