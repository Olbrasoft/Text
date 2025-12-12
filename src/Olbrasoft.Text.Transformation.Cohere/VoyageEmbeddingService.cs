using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Olbrasoft.Text.Transformation.Abstractions;

namespace Olbrasoft.Text.Transformation.Cohere;

/// <summary>
/// Voyage AI embedding service implementation.
/// Uses voyage-multilingual-2 model which supports Czech (1024 dimensions).
/// </summary>
public class VoyageEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly VoyageSettings _settings;
    private readonly ILogger<VoyageEmbeddingService> _logger;

    private const string VoyageApiUrl = "https://api.voyageai.com/v1/embeddings";

    public VoyageEmbeddingService(
        HttpClient httpClient,
        IOptions<VoyageSettings> settings,
        ILogger<VoyageEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            _logger.LogInformation("Voyage embedding service initialized");
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
            _logger.LogWarning("Voyage API key is not configured");
            return null;
        }

        var stopwatch = Stopwatch.StartNew();
        var inputTypeString = inputType == EmbeddingInputType.Query ? "query" : "document";

        try
        {
            var request = new VoyageEmbedRequest
            {
                Input = [text],
                Model = _settings.Model,
                InputType = inputTypeString
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, VoyageApiUrl);
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);
            httpRequest.Content = JsonContent.Create(request);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Voyage API error: status={StatusCode}, latency={Latency}ms, error={Error}",
                    (int)response.StatusCode, stopwatch.ElapsedMilliseconds, errorContent);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<VoyageEmbedResponse>(cancellationToken);

            if (result?.Data?.Count > 0)
            {
                var embedding = result.Data[0].Embedding.ToArray();
                _logger.LogInformation("Voyage embed: type={InputType}, status={StatusCode}, latency={Latency}ms, dim={Dimensions}",
                    inputTypeString, (int)response.StatusCode, stopwatch.ElapsedMilliseconds, embedding.Length);
                return embedding;
            }

            _logger.LogWarning("Voyage response did not contain embeddings");
            return null;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Voyage error: latency={Latency}ms", stopwatch.ElapsedMilliseconds);
            return null;
        }
    }

    private class VoyageEmbedRequest
    {
        [JsonPropertyName("input")]
        public string[] Input { get; set; } = [];

        [JsonPropertyName("model")]
        public string Model { get; set; } = "voyage-multilingual-2";

        [JsonPropertyName("input_type")]
        public string InputType { get; set; } = "document";
    }

    private class VoyageEmbedResponse
    {
        [JsonPropertyName("data")]
        public List<VoyageEmbeddingData>? Data { get; set; }
    }

    private class VoyageEmbeddingData
    {
        [JsonPropertyName("embedding")]
        public List<float> Embedding { get; set; } = [];
    }
}

/// <summary>
/// Voyage AI embedding settings.
/// </summary>
public class VoyageSettings
{
    /// <summary>
    /// Voyage AI API key.
    /// </summary>
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// Voyage model to use. Default: voyage-multilingual-2 (1024 dimensions).
    /// </summary>
    public string Model { get; set; } = "voyage-multilingual-2";

    /// <summary>
    /// Vector dimensions. Default: 1024.
    /// </summary>
    public int Dimensions { get; set; } = 1024;
}
