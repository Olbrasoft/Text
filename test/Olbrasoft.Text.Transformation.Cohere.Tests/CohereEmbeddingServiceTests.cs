using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Olbrasoft.Text.Transformation.Abstractions;

namespace Olbrasoft.Text.Transformation.Cohere.Tests;

public class CohereEmbeddingServiceTests
{
    private readonly Mock<ILogger<CohereEmbeddingService>> _loggerMock;

    public CohereEmbeddingServiceTests()
    {
        _loggerMock = new Mock<ILogger<CohereEmbeddingService>>();
    }

    private static IOptions<EmbeddingSettings> CreateSettings(params string[] keys)
    {
        return Options.Create(new EmbeddingSettings
        {
            Provider = EmbeddingProvider.Cohere,
            CohereApiKeys = keys,
            CohereModel = "embed-multilingual-v3.0",
            Dimensions = 1024
        });
    }

    private static HttpClient CreateMockedHttpClient(HttpStatusCode statusCode, string responseContent)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent)
            });

        return new HttpClient(handlerMock.Object);
    }

    [Fact]
    public void IsConfigured_WithNoKeys_ReturnsFalse()
    {
        var settings = CreateSettings();
        var httpClient = new HttpClient();
        var service = new CohereEmbeddingService(httpClient, settings, _loggerMock.Object);

        Assert.False(service.IsConfigured);
    }

    [Fact]
    public void IsConfigured_WithKeys_ReturnsTrue()
    {
        var settings = CreateSettings("test-key-1");
        var httpClient = new HttpClient();
        var service = new CohereEmbeddingService(httpClient, settings, _loggerMock.Object);

        Assert.True(service.IsConfigured);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithEmptyText_ReturnsNull()
    {
        var settings = CreateSettings("test-key");
        var httpClient = new HttpClient();
        var service = new CohereEmbeddingService(httpClient, settings, _loggerMock.Object);

        var result = await service.GenerateEmbeddingAsync("");

        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithWhitespaceText_ReturnsNull()
    {
        var settings = CreateSettings("test-key");
        var httpClient = new HttpClient();
        var service = new CohereEmbeddingService(httpClient, settings, _loggerMock.Object);

        var result = await service.GenerateEmbeddingAsync("   ");

        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithNoKeys_ReturnsNull()
    {
        var settings = CreateSettings();
        var httpClient = new HttpClient();
        var service = new CohereEmbeddingService(httpClient, settings, _loggerMock.Object);

        var result = await service.GenerateEmbeddingAsync("test text");

        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithSuccessfulResponse_ReturnsEmbedding()
    {
        var settings = CreateSettings("test-key");
        var responseJson = JsonSerializer.Serialize(new
        {
            id = "test-id",
            embeddings = new
            {
                @float = new[] { new[] { 0.1f, 0.2f, 0.3f } }
            }
        });
        var httpClient = CreateMockedHttpClient(HttpStatusCode.OK, responseJson);
        var service = new CohereEmbeddingService(httpClient, settings, _loggerMock.Object);

        var result = await service.GenerateEmbeddingAsync("test text");

        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        Assert.Equal(0.1f, result[0]);
        Assert.Equal(0.2f, result[1]);
        Assert.Equal(0.3f, result[2]);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithUnauthorizedResponse_ReturnsNull()
    {
        var settings = CreateSettings("invalid-key");
        var httpClient = CreateMockedHttpClient(HttpStatusCode.Unauthorized, "Invalid API key");
        var service = new CohereEmbeddingService(httpClient, settings, _loggerMock.Object);

        var result = await service.GenerateEmbeddingAsync("test text");

        Assert.Null(result);
    }

    [Fact]
    public async Task IsAvailableAsync_WhenNotConfigured_ReturnsFalse()
    {
        var settings = CreateSettings();
        var httpClient = new HttpClient();
        var service = new CohereEmbeddingService(httpClient, settings, _loggerMock.Object);

        var result = await service.IsAvailableAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task IsAvailableAsync_WhenApiSucceeds_ReturnsTrue()
    {
        var settings = CreateSettings("test-key");
        var responseJson = JsonSerializer.Serialize(new
        {
            id = "test-id",
            embeddings = new
            {
                @float = new[] { new[] { 0.1f, 0.2f, 0.3f } }
            }
        });
        var httpClient = CreateMockedHttpClient(HttpStatusCode.OK, responseJson);
        var service = new CohereEmbeddingService(httpClient, settings, _loggerMock.Object);

        var result = await service.IsAvailableAsync();

        Assert.True(result);
    }

    [Theory]
    [InlineData(EmbeddingInputType.Document)]
    [InlineData(EmbeddingInputType.Query)]
    public async Task GenerateEmbeddingAsync_WithDifferentInputTypes_ReturnsEmbedding(EmbeddingInputType inputType)
    {
        var settings = CreateSettings("test-key");
        var responseJson = JsonSerializer.Serialize(new
        {
            id = "test-id",
            embeddings = new
            {
                @float = new[] { new[] { 0.5f, 0.6f } }
            }
        });
        var httpClient = CreateMockedHttpClient(HttpStatusCode.OK, responseJson);
        var service = new CohereEmbeddingService(httpClient, settings, _loggerMock.Object);

        var result = await service.GenerateEmbeddingAsync("test text", inputType);

        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
    }
}
