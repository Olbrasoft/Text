using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Olbrasoft.Text.Transformation.Abstractions;

namespace Olbrasoft.Text.Transformation.OpenAICompatible.Tests;

public class OpenAICompatibleTranslationServiceTests
{
    private readonly Mock<ILogger<OpenAICompatibleTranslationService>> _loggerMock;

    public OpenAICompatibleTranslationServiceTests()
    {
        _loggerMock = new Mock<ILogger<OpenAICompatibleTranslationService>>();
    }

    private static IOptions<AiProvidersSettings> CreateProvidersSettings(params string[] cerebrasKeys)
    {
        return Options.Create(new AiProvidersSettings
        {
            Cerebras = new AiProviderConfig
            {
                Endpoint = "https://api.cerebras.ai/v1/",
                Models = ["qwen-3-32b"],
                Keys = cerebrasKeys
            },
            Groq = new AiProviderConfig()
        });
    }

    private static IOptions<TranslationSettings> CreateTranslationSettings()
    {
        return Options.Create(new TranslationSettings
        {
            MaxTokens = 300,
            Temperature = 0.2,
            TargetLanguage = "Czech",
            SystemPrompt = "Translate to Czech."
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
    public async Task TranslateToCzechAsync_WithEmptyText_ReturnsFail()
    {
        var providerSettings = CreateProvidersSettings("test-key");
        var transSettings = CreateTranslationSettings();
        var httpClient = new HttpClient();
        var service = new OpenAICompatibleTranslationService(httpClient, providerSettings, transSettings, _loggerMock.Object);

        var result = await service.TranslateToCzechAsync("");

        Assert.False(result.Success);
        Assert.Contains("No text", result.Error);
    }

    [Fact]
    public async Task TranslateToCzechAsync_WithWhitespace_ReturnsFail()
    {
        var providerSettings = CreateProvidersSettings("test-key");
        var transSettings = CreateTranslationSettings();
        var httpClient = new HttpClient();
        var service = new OpenAICompatibleTranslationService(httpClient, providerSettings, transSettings, _loggerMock.Object);

        var result = await service.TranslateToCzechAsync("   ");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task TranslateToCzechAsync_WithNoProviders_ReturnsFail()
    {
        var providerSettings = CreateProvidersSettings();
        var transSettings = CreateTranslationSettings();
        var httpClient = new HttpClient();
        var service = new OpenAICompatibleTranslationService(httpClient, providerSettings, transSettings, _loggerMock.Object);

        var result = await service.TranslateToCzechAsync("Hello world");

        Assert.False(result.Success);
        Assert.Contains("No providers", result.Error);
    }

    [Fact]
    public async Task TranslateToCzechAsync_WithSuccessfulResponse_ReturnsTranslation()
    {
        var providerSettings = CreateProvidersSettings("test-key");
        var transSettings = CreateTranslationSettings();
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { content = "Ahoj světe" } }
            }
        });
        var httpClient = CreateMockedHttpClient(HttpStatusCode.OK, responseJson);
        var service = new OpenAICompatibleTranslationService(httpClient, providerSettings, transSettings, _loggerMock.Object);

        var result = await service.TranslateToCzechAsync("Hello world");

        Assert.True(result.Success);
        Assert.Equal("Ahoj světe", result.Translation);
        Assert.Equal("Cerebras", result.Provider);
    }

    [Fact]
    public async Task TranslateToCzechAsync_WithHttpError_ReturnsFail()
    {
        var providerSettings = CreateProvidersSettings("test-key");
        var transSettings = CreateTranslationSettings();
        var httpClient = CreateMockedHttpClient(HttpStatusCode.ServiceUnavailable, "Service unavailable");
        var service = new OpenAICompatibleTranslationService(httpClient, providerSettings, transSettings, _loggerMock.Object);

        var result = await service.TranslateToCzechAsync("Hello");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task TranslateToCzechAsync_WithEmptyResponse_ReturnsFail()
    {
        var providerSettings = CreateProvidersSettings("test-key");
        var transSettings = CreateTranslationSettings();
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { content = "" } }
            }
        });
        var httpClient = CreateMockedHttpClient(HttpStatusCode.OK, responseJson);
        var service = new OpenAICompatibleTranslationService(httpClient, providerSettings, transSettings, _loggerMock.Object);

        var result = await service.TranslateToCzechAsync("Hello");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task TranslateToCzechAsync_StripsThinkTags_ReturnsTranslation()
    {
        var providerSettings = CreateProvidersSettings("test-key");
        var transSettings = CreateTranslationSettings();
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { content = "<think>Let me translate...</think>Překlad textu." } }
            }
        });
        var httpClient = CreateMockedHttpClient(HttpStatusCode.OK, responseJson);
        var service = new OpenAICompatibleTranslationService(httpClient, providerSettings, transSettings, _loggerMock.Object);

        var result = await service.TranslateToCzechAsync("Text translation");

        Assert.True(result.Success);
        Assert.Equal("Překlad textu.", result.Translation);
    }

    [Fact]
    public async Task TranslateToCzechAsync_WithTruncatedThinkBlock_ReturnsFail()
    {
        var providerSettings = CreateProvidersSettings("test-key");
        var transSettings = CreateTranslationSettings();
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { content = "<think>Thinking..." } }
            }
        });
        var httpClient = CreateMockedHttpClient(HttpStatusCode.OK, responseJson);
        var service = new OpenAICompatibleTranslationService(httpClient, providerSettings, transSettings, _loggerMock.Object);

        var result = await service.TranslateToCzechAsync("Hello");

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }
}
