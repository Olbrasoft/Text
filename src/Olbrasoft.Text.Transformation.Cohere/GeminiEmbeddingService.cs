using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Olbrasoft.Text.Transformation.Abstractions;

namespace Olbrasoft.Text.Transformation.Cohere;

/// <summary>
/// Google Gemini embedding service implementation.
/// Uses text-embedding-004 model which supports Czech (768 dimensions, FREE tier).
/// </summary>
public class GeminiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiEmbeddingService> _logger;

    public GeminiEmbeddingService(
        HttpClient httpClient,
        IOptions<GeminiSettings> settings,
        ILogger<GeminiEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            _logger.LogInformation("Gemini embedding service initialized");
        }
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_settings.ApiKey);

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return false;
        }

        try
        {
            var embedding = await GenerateEmbeddingAsync("test", EmbeddingInputType.Query, cancellationToken);
            return embedding != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<float[]?> GenerateEmbeddingAsync(
        string text,
        EmbeddingInputType inputType = EmbeddingInputType.Document,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (!IsConfigured)
        {
            _logger.LogWarning("Gemini API key is not configured");
            return null;
        }

        var stopwatch = Stopwatch.StartNew();
        var taskType = inputType == EmbeddingInputType.Query
            ? "RETRIEVAL_QUERY"
            : "RETRIEVAL_DOCUMENT";

        try
        {
            var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.Model}:embedContent?key={_settings.ApiKey}";

            var request = new GeminiEmbedRequest
            {
                Model = $"models/{_settings.Model}",
                Content = new GeminiContent
                {
                    Parts = [new GeminiPart { Text = text }]
                },
                TaskType = taskType
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            httpRequest.Content = JsonContent.Create(request);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Gemini API error: status={StatusCode}, latency={Latency}ms, error={Error}",
                    (int)response.StatusCode, stopwatch.ElapsedMilliseconds, errorContent);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<GeminiEmbedResponse>(cancellationToken);

            if (result?.Embedding?.Values?.Count > 0)
            {
                var embedding = result.Embedding.Values.ToArray();
                _logger.LogInformation("Gemini embed: taskType={TaskType}, status={StatusCode}, latency={Latency}ms, dim={Dimensions}",
                    taskType, (int)response.StatusCode, stopwatch.ElapsedMilliseconds, embedding.Length);
                return embedding;
            }

            _logger.LogWarning("Gemini response did not contain embeddings");
            return null;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Gemini error: latency={Latency}ms", stopwatch.ElapsedMilliseconds);
            return null;
        }
    }

    private class GeminiEmbedRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("content")]
        public GeminiContent Content { get; set; } = new();

        [JsonPropertyName("taskType")]
        public string TaskType { get; set; } = "RETRIEVAL_DOCUMENT";
    }

    private class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = [];
    }

    private class GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";
    }

    private class GeminiEmbedResponse
    {
        [JsonPropertyName("embedding")]
        public GeminiEmbedding? Embedding { get; set; }
    }

    private class GeminiEmbedding
    {
        [JsonPropertyName("values")]
        public List<float> Values { get; set; } = [];
    }
}

/// <summary>
/// Google Gemini embedding settings.
/// </summary>
public class GeminiSettings
{
    /// <summary>
    /// Google API key.
    /// </summary>
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// Gemini model to use. Default: text-embedding-004 (768 dimensions, FREE).
    /// </summary>
    public string Model { get; set; } = "text-embedding-004";

    /// <summary>
    /// Vector dimensions. Default: 768.
    /// </summary>
    public int Dimensions { get; set; } = 768;
}
