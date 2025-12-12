using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Olbrasoft.Text.Transformation.Abstractions;

namespace Olbrasoft.Text.Transformation.Cohere.Tests;

public class CohereTranslationServiceTests
{
    private readonly Mock<ILogger<CohereTranslationService>> _loggerMock;

    public CohereTranslationServiceTests()
    {
        _loggerMock = new Mock<ILogger<CohereTranslationService>>();
    }

    private static IOptions<AiProvidersSettings> CreateSettings(params string[] keys)
    {
        return Options.Create(new AiProvidersSettings
        {
            Cohere = new CohereProviderConfig
            {
                Endpoint = "https://api.cohere.com/v2/",
                Keys = keys,
                TranslationModels = ["command-a-translate-08-2025"]
            }
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
        var settings = CreateSettings("test-key");
        var httpClient = new HttpClient();
        var service = new CohereTranslationService(httpClient, settings, _loggerMock.Object);

        var result = await service.TranslateToCzechAsync("");

        Assert.False(result.Success);
        Assert.Contains("No text", result.Error);
    }

    [Fact]
    public async Task TranslateToCzechAsync_WithWhitespace_ReturnsFail()
    {
        var settings = CreateSettings("test-key");
        var httpClient = new HttpClient();
        var service = new CohereTranslationService(httpClient, settings, _loggerMock.Object);

        var result = await service.TranslateToCzechAsync("   ");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task TranslateToCzechAsync_WithNoKeys_ReturnsFail()
    {
        var settings = CreateSettings();
        var httpClient = new HttpClient();
        var service = new CohereTranslationService(httpClient, settings, _loggerMock.Object);

        var result = await service.TranslateToCzechAsync("Hello world");

        Assert.False(result.Success);
        Assert.Contains("No Cohere keys", result.Error);
    }

    [Fact]
    public async Task TranslateToCzechAsync_WithSuccessfulResponse_ReturnsTranslation()
    {
        var settings = CreateSettings("test-key");
        var responseJson = JsonSerializer.Serialize(new
        {
            message = new
            {
                content = new[]
                {
                    new { type = "text", text = "Ahoj světe" }
                }
            }
        });
        var httpClient = CreateMockedHttpClient(HttpStatusCode.OK, responseJson);
        var service = new CohereTranslationService(httpClient, settings, _loggerMock.Object);

        var result = await service.TranslateToCzechAsync("Hello world");

        Assert.True(result.Success);
        Assert.Equal("Ahoj světe", result.Translation);
        Assert.Equal("Cohere", result.Provider);
    }

    [Fact]
    public async Task TranslateToCzechAsync_WithHttpError_ReturnsFail()
    {
        var settings = CreateSettings("test-key");
        var httpClient = CreateMockedHttpClient(HttpStatusCode.InternalServerError, "Server error");
        var service = new CohereTranslationService(httpClient, settings, _loggerMock.Object);

        var result = await service.TranslateToCzechAsync("Hello world");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task TranslateToCzechAsync_WithEmptyResponse_ReturnsFail()
    {
        var settings = CreateSettings("test-key");
        var responseJson = JsonSerializer.Serialize(new
        {
            message = new
            {
                content = Array.Empty<object>()
            }
        });
        var httpClient = CreateMockedHttpClient(HttpStatusCode.OK, responseJson);
        var service = new CohereTranslationService(httpClient, settings, _loggerMock.Object);

        var result = await service.TranslateToCzechAsync("Hello world");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task TranslateToCzechAsync_WithUnauthorized_ReturnsFail()
    {
        var settings = CreateSettings("invalid-key");
        var httpClient = CreateMockedHttpClient(HttpStatusCode.Unauthorized, "Invalid API key");
        var service = new CohereTranslationService(httpClient, settings, _loggerMock.Object);

        var result = await service.TranslateToCzechAsync("Hello");

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }
}
