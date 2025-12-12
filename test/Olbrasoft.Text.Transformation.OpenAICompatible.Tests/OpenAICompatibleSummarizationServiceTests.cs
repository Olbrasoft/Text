using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Olbrasoft.Text.Transformation.Abstractions;

namespace Olbrasoft.Text.Transformation.OpenAICompatible.Tests;

public class OpenAICompatibleSummarizationServiceTests
{
    private readonly Mock<ILogger<OpenAICompatibleSummarizationService>> _loggerMock;

    public OpenAICompatibleSummarizationServiceTests()
    {
        _loggerMock = new Mock<ILogger<OpenAICompatibleSummarizationService>>();
    }

    private static IOptions<AiProvidersSettings> CreateProvidersSettings(params string[] cerebrasKeys)
    {
        return Options.Create(new AiProvidersSettings
        {
            Cerebras = new AiProviderConfig
            {
                Endpoint = "https://api.cerebras.ai/v1/",
                Models = ["llama-3.3-70b"],
                Keys = cerebrasKeys
            },
            Groq = new AiProviderConfig()
        });
    }

    private static IOptions<SummarizationSettings> CreateSummarizationSettings()
    {
        return Options.Create(new SummarizationSettings
        {
            MaxTokens = 150,
            Temperature = 0.3,
            SystemPrompt = "Summarize the issue."
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
    public async Task SummarizeAsync_WithEmptyContent_ReturnsFail()
    {
        var providerSettings = CreateProvidersSettings("test-key");
        var summSettings = CreateSummarizationSettings();
        var httpClient = new HttpClient();
        var service = new OpenAICompatibleSummarizationService(httpClient, providerSettings, summSettings, _loggerMock.Object);

        var result = await service.SummarizeAsync("");

        Assert.False(result.Success);
        Assert.Contains("No content", result.Error);
    }

    [Fact]
    public async Task SummarizeAsync_WithWhitespace_ReturnsFail()
    {
        var providerSettings = CreateProvidersSettings("test-key");
        var summSettings = CreateSummarizationSettings();
        var httpClient = new HttpClient();
        var service = new OpenAICompatibleSummarizationService(httpClient, providerSettings, summSettings, _loggerMock.Object);

        var result = await service.SummarizeAsync("   ");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SummarizeAsync_WithNoProviders_ReturnsFail()
    {
        var providerSettings = CreateProvidersSettings();
        var summSettings = CreateSummarizationSettings();
        var httpClient = new HttpClient();
        var service = new OpenAICompatibleSummarizationService(httpClient, providerSettings, summSettings, _loggerMock.Object);

        var result = await service.SummarizeAsync("Test content");

        Assert.False(result.Success);
        Assert.Contains("No AI providers", result.Error);
    }

    [Fact]
    public async Task SummarizeAsync_WithSuccessfulResponse_ReturnsSummary()
    {
        var providerSettings = CreateProvidersSettings("test-key");
        var summSettings = CreateSummarizationSettings();
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { content = "This is a summary." } }
            }
        });
        var httpClient = CreateMockedHttpClient(HttpStatusCode.OK, responseJson);
        var service = new OpenAICompatibleSummarizationService(httpClient, providerSettings, summSettings, _loggerMock.Object);

        var result = await service.SummarizeAsync("Test content to summarize");

        Assert.True(result.Success);
        Assert.Equal("This is a summary.", result.Summary);
        Assert.Equal("Cerebras", result.Provider);
    }

    [Fact]
    public async Task SummarizeAsync_WithHttpError_ReturnsFail()
    {
        var providerSettings = CreateProvidersSettings("test-key");
        var summSettings = CreateSummarizationSettings();
        var httpClient = CreateMockedHttpClient(HttpStatusCode.InternalServerError, "Error");
        var service = new OpenAICompatibleSummarizationService(httpClient, providerSettings, summSettings, _loggerMock.Object);

        var result = await service.SummarizeAsync("Test content");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SummarizeAsync_WithEmptyResponse_ReturnsFail()
    {
        var providerSettings = CreateProvidersSettings("test-key");
        var summSettings = CreateSummarizationSettings();
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { content = "" } }
            }
        });
        var httpClient = CreateMockedHttpClient(HttpStatusCode.OK, responseJson);
        var service = new OpenAICompatibleSummarizationService(httpClient, providerSettings, summSettings, _loggerMock.Object);

        var result = await service.SummarizeAsync("Test content");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SummarizeAsync_StripsThinkTags_ReturnsSummary()
    {
        var providerSettings = CreateProvidersSettings("test-key");
        var summSettings = CreateSummarizationSettings();
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { content = "<think>Internal thoughts</think>Clean summary here." } }
            }
        });
        var httpClient = CreateMockedHttpClient(HttpStatusCode.OK, responseJson);
        var service = new OpenAICompatibleSummarizationService(httpClient, providerSettings, summSettings, _loggerMock.Object);

        var result = await service.SummarizeAsync("Test content");

        Assert.True(result.Success);
        Assert.Equal("Clean summary here.", result.Summary);
    }

    [Fact]
    public async Task SummarizeAsync_WithTruncatedThinkBlock_ReturnsFail()
    {
        var providerSettings = CreateProvidersSettings("test-key");
        var summSettings = CreateSummarizationSettings();
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { content = "<think>Thinking but never closes..." } }
            }
        });
        var httpClient = CreateMockedHttpClient(HttpStatusCode.OK, responseJson);
        var service = new OpenAICompatibleSummarizationService(httpClient, providerSettings, summSettings, _loggerMock.Object);

        var result = await service.SummarizeAsync("Test content");

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }
}
