using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Olbrasoft.Text.Transformation.Abstractions;

namespace Olbrasoft.Text.Transformation.OpenAICompatible;

/// <summary>
/// OpenAI-compatible translation service using models with Czech support (e.g., qwen-3-32b).
/// </summary>
public class OpenAICompatibleTranslationService : ITranslationService
{
    private readonly HttpClient _httpClient;
    private readonly AiProvidersSettings _providers;
    private readonly TranslationSettings _translation;
    private readonly ILogger<OpenAICompatibleTranslationService> _logger;

    // Thread-safe rotation
    private static int _fallbackIndex;

    // Pre-built fallback combinations (Cerebras/Groq with Czech-capable models)
    private readonly List<TranslationProvider> _providers_list;

    public OpenAICompatibleTranslationService(
        HttpClient httpClient,
        IOptions<AiProvidersSettings> providers,
        IOptions<TranslationSettings> translation,
        ILogger<OpenAICompatibleTranslationService> logger)
    {
        _httpClient = httpClient;
        _providers = providers.Value;
        _translation = translation.Value;
        _logger = logger;

        _providers_list = BuildProvidersList();
    }

    private List<TranslationProvider> BuildProvidersList()
    {
        var list = new List<TranslationProvider>();

        // Add Cerebras with Czech-capable models (qwen-3-32b has official Czech support)
        var cerebrasKeys = _providers.Cerebras.Keys.Where(k => !string.IsNullOrEmpty(k)).ToList();
        foreach (var key in cerebrasKeys)
        {
            // qwen-3-32b officially supports 119 languages including Czech
            list.Add(new TranslationProvider("Cerebras", _providers.Cerebras.Endpoint, key, "qwen-3-32b"));
        }

        // Add Groq with Czech-capable models
        var groqKeys = _providers.Groq.Keys.Where(k => !string.IsNullOrEmpty(k)).ToList();
        foreach (var key in groqKeys)
        {
            list.Add(new TranslationProvider("Groq", _providers.Groq.Endpoint, key, "qwen/qwen3-32b"));
        }

        _logger.LogInformation("Built {Count} translation providers", list.Count);
        return list;
    }

    public async Task<TranslationResult> TranslateToCzechAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return TranslationResult.Fail("No text to translate");
        }

        if (_providers_list.Count == 0)
        {
            return TranslationResult.Fail("No providers configured");
        }

        var startIndex = Interlocked.Increment(ref _fallbackIndex) % _providers_list.Count;
        var tried = 0;

        while (tried < _providers_list.Count)
        {
            var index = (startIndex + tried) % _providers_list.Count;
            var provider = _providers_list[index];

            try
            {
                var result = await CallOpenAiCompatibleAsync(provider, text, cancellationToken);
                if (result.Success)
                {
                    return result;
                }
                _logger.LogWarning("Translation failed with {Provider}/{Model}: {Error}",
                    provider.Name, provider.Model, result.Error);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception during translation with {Provider}/{Model}",
                    provider.Name, provider.Model);
            }

            tried++;
        }

        return TranslationResult.Fail("All translation providers failed");
    }

    private async Task<TranslationResult> CallOpenAiCompatibleAsync(
        TranslationProvider provider,
        string text,
        CancellationToken cancellationToken)
    {
        var request = new OpenAiTranslationRequest
        {
            Model = provider.Model,
            Messages =
            [
                new OpenAiTranslationMessage { Role = "system", Content = _translation.SystemPrompt },
                new OpenAiTranslationMessage { Role = "user", Content = text }
            ],
            MaxTokens = _translation.MaxTokens,
            Temperature = _translation.Temperature
        };

        var json = JsonSerializer.Serialize(request, TranslationJsonContext.Default.OpenAiTranslationRequest);
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, provider.Endpoint + "chat/completions");
        httpRequest.Content = httpContent;
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);

        _logger.LogDebug("Trying translation with {Provider}/{Model}", provider.Name, provider.Model);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return TranslationResult.Fail($"HTTP {(int)response.StatusCode}: {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var openAiResponse = JsonSerializer.Deserialize(responseBody, TranslationJsonContext.Default.OpenAiTranslationResponse);

        var translation = openAiResponse?.Choices?.FirstOrDefault()?.Message?.Content;

        if (string.IsNullOrEmpty(translation))
        {
            return TranslationResult.Fail("Empty response from API");
        }

        // Strip <think>...</think> tags from chain-of-thought models
        translation = StripThinkingTags(translation);

        if (string.IsNullOrWhiteSpace(translation))
        {
            return TranslationResult.Fail("Response truncated (increase MaxTokens)");
        }

        _logger.LogInformation("Translation successful with {Provider}/{Model}", provider.Name, provider.Model);
        return TranslationResult.Ok(translation, provider.Name, provider.Model);
    }

    private static string StripThinkingTags(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        var result = System.Text.RegularExpressions.Regex.Replace(
            content,
            @"<think>[\s\S]*?</think>",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (result.TrimStart().StartsWith("<think>", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return result.Trim();
    }

    private record TranslationProvider(string Name, string Endpoint, string ApiKey, string Model);
}

// OpenAI-compatible models for translation
internal class OpenAiTranslationRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public OpenAiTranslationMessage[] Messages { get; set; } = [];

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }
}

internal class OpenAiTranslationMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

internal class OpenAiTranslationResponse
{
    [JsonPropertyName("choices")]
    public OpenAiTranslationChoice[]? Choices { get; set; }
}

internal class OpenAiTranslationChoice
{
    [JsonPropertyName("message")]
    public OpenAiTranslationMessage? Message { get; set; }
}

[JsonSerializable(typeof(OpenAiTranslationRequest))]
[JsonSerializable(typeof(OpenAiTranslationResponse))]
internal partial class TranslationJsonContext : JsonSerializerContext
{
}
