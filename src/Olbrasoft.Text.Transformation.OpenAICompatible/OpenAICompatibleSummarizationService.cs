using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Olbrasoft.Text.Transformation.Abstractions;

namespace Olbrasoft.Text.Transformation.OpenAICompatible;

/// <summary>
/// AI summarization service with 3-level rotation: Provider → Key → Model.
/// Rotation order cycles through all combinations to maximize free tier usage.
/// </summary>
public class OpenAICompatibleSummarizationService : ISummarizationService
{
    private readonly HttpClient _httpClient;
    private readonly AiProvidersSettings _providers;
    private readonly SummarizationSettings _summarization;
    private readonly ILogger<OpenAICompatibleSummarizationService> _logger;

    // Static rotation state - persists across all instances (service can be transient/scoped)
    // Thread-safe with Interlocked
    private static int _rotationIndex;

    // Pre-built list of all provider/key/model combinations
    private readonly List<ProviderKeyModel> _combinations;

    public OpenAICompatibleSummarizationService(
        HttpClient httpClient,
        IOptions<AiProvidersSettings> providers,
        IOptions<SummarizationSettings> summarization,
        ILogger<OpenAICompatibleSummarizationService> logger)
    {
        _httpClient = httpClient;
        _providers = providers.Value;
        _summarization = summarization.Value;
        _logger = logger;

        _combinations = BuildCombinations();
    }

    /// <summary>
    /// Builds all provider/key/model combinations in rotation order.
    /// Order: For each model level, cycle through (Cerebras+Key1, Groq+Key1, Cerebras+Key2, Groq+Key2).
    /// </summary>
    private List<ProviderKeyModel> BuildCombinations()
    {
        var combinations = new List<ProviderKeyModel>();

        var cerebrasKeys = _providers.Cerebras.Keys.Where(k => !string.IsNullOrEmpty(k)).ToList();
        var groqKeys = _providers.Groq.Keys.Where(k => !string.IsNullOrEmpty(k)).ToList();
        var cerebrasModels = _providers.Cerebras.Models;
        var groqModels = _providers.Groq.Models;

        var maxModelIndex = Math.Max(cerebrasModels.Length, groqModels.Length);

        for (var modelIndex = 0; modelIndex < maxModelIndex; modelIndex++)
        {
            var maxKeyIndex = Math.Max(cerebrasKeys.Count, groqKeys.Count);

            for (var keyIndex = 0; keyIndex < maxKeyIndex; keyIndex++)
            {
                // Cerebras with current key and model
                if (keyIndex < cerebrasKeys.Count && modelIndex < cerebrasModels.Length)
                {
                    combinations.Add(new ProviderKeyModel(
                        "Cerebras",
                        _providers.Cerebras.Endpoint,
                        cerebrasKeys[keyIndex],
                        cerebrasModels[modelIndex]));
                }

                // Groq with current key and model
                if (keyIndex < groqKeys.Count && modelIndex < groqModels.Length)
                {
                    combinations.Add(new ProviderKeyModel(
                        "Groq",
                        _providers.Groq.Endpoint,
                        groqKeys[keyIndex],
                        groqModels[keyIndex]));
                }
            }
        }

        _logger.LogInformation("Built {Count} provider/key/model combinations for rotation", combinations.Count);
        return combinations;
    }

    public async Task<SummarizationResult> SummarizeAsync(string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return SummarizationResult.Fail("No content to summarize");
        }

        if (_combinations.Count == 0)
        {
            _logger.LogWarning("No AI providers configured for summarization");
            return SummarizationResult.Fail("No AI providers configured");
        }

        // Try each combination starting from current rotation index
        var startIndex = GetAndAdvanceRotationIndex();
        var triedCount = 0;

        while (triedCount < _combinations.Count)
        {
            var index = (startIndex + triedCount) % _combinations.Count;
            var combo = _combinations[index];

            try
            {
                var result = await TrySummarizeAsync(combo, content, cancellationToken);
                if (result.Success)
                {
                    return result;
                }

                _logger.LogWarning("Summarization failed with {Provider}/{Model}: {Error}",
                    combo.ProviderName, combo.Model, result.Error);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception during summarization with {Provider}/{Model}",
                    combo.ProviderName, combo.Model);
            }

            triedCount++;
        }

        return SummarizationResult.Fail("All providers failed");
    }

    private int GetAndAdvanceRotationIndex()
    {
        // Thread-safe increment without lock
        var count = Math.Max(1, _combinations.Count);
        var newIndex = Interlocked.Increment(ref _rotationIndex);
        return (newIndex - 1) % count; // -1 because Increment returns NEW value
    }

    private async Task<SummarizationResult> TrySummarizeAsync(
        ProviderKeyModel combo,
        string content,
        CancellationToken cancellationToken)
    {
        var request = new OpenAiRequest
        {
            Model = combo.Model,
            Messages =
            [
                new OpenAiMessage { Role = "system", Content = _summarization.SystemPrompt },
                new OpenAiMessage { Role = "user", Content = $"Summarize this GitHub issue:\n\n{content}" }
            ],
            MaxTokens = _summarization.MaxTokens,
            Temperature = _summarization.Temperature
        };

        var json = JsonSerializer.Serialize(request, SummarizationJsonContext.Default.OpenAiRequest);
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, combo.Endpoint + "chat/completions");
        httpRequest.Content = httpContent;
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", combo.ApiKey);

        _logger.LogDebug("Trying summarization with {Provider}/{Model}", combo.ProviderName, combo.Model);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return SummarizationResult.Fail($"HTTP {(int)response.StatusCode}: {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var openAiResponse = JsonSerializer.Deserialize(responseBody, SummarizationJsonContext.Default.OpenAiResponse);

        var summary = openAiResponse?.Choices?.FirstOrDefault()?.Message?.Content;

        if (string.IsNullOrEmpty(summary))
        {
            return SummarizationResult.Fail("Empty response from API");
        }

        // Strip <think>...</think> tags from chain-of-thought models (e.g., qwen-3)
        summary = StripThinkingTags(summary);

        // If stripping resulted in empty content, model was truncated mid-thinking
        if (string.IsNullOrWhiteSpace(summary))
        {
            _logger.LogWarning("Summary empty after stripping think tags - response was likely truncated");
            return SummarizationResult.Fail("Response truncated (increase MaxTokens)");
        }

        _logger.LogInformation("Summarization successful with {Provider}/{Model}", combo.ProviderName, combo.Model);
        return SummarizationResult.Ok(summary, combo.ProviderName, combo.Model);
    }

    private record ProviderKeyModel(string ProviderName, string Endpoint, string ApiKey, string Model);

    /// <summary>
    /// Strips &lt;think&gt;...&lt;/think&gt; tags from chain-of-thought model outputs.
    /// Also handles truncated think blocks that never close.
    /// </summary>
    private static string StripThinkingTags(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // Remove complete <think>...</think> blocks (including multiline)
        var result = System.Text.RegularExpressions.Regex.Replace(
            content,
            @"<think>[\s\S]*?</think>",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Handle truncated think block (starts with <think> but never closes)
        // If content still starts with <think> (truncated response), return empty string
        if (result.TrimStart().StartsWith("<think>", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return result.Trim();
    }
}

// OpenAI-compatible API models with source generation for AOT compatibility
internal class OpenAiRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public OpenAiMessage[] Messages { get; set; } = [];

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }
}

internal class OpenAiMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

internal class OpenAiResponse
{
    [JsonPropertyName("choices")]
    public OpenAiChoice[]? Choices { get; set; }
}

internal class OpenAiChoice
{
    [JsonPropertyName("message")]
    public OpenAiMessage? Message { get; set; }
}

[JsonSerializable(typeof(OpenAiRequest))]
[JsonSerializable(typeof(OpenAiResponse))]
internal partial class SummarizationJsonContext : JsonSerializerContext
{
}
