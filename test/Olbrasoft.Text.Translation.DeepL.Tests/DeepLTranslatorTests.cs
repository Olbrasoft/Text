using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace Olbrasoft.Text.Translation.DeepL.Tests;

public class DeepLTranslatorTests
{
    private readonly Mock<ILogger<DeepLTranslator>> _loggerMock = new();

    private DeepLTranslator CreateTranslator(
        HttpMessageHandler handler,
        string apiKey = "test-key",
        string endpoint = "https://api-free.deepl.com/v2/")
    {
        var settings = Options.Create(new DeepLSettings
        {
            ApiKey = apiKey,
            Endpoint = endpoint
        });

        var httpClient = new HttpClient(handler);
        return new DeepLTranslator(httpClient, settings, _loggerMock.Object);
    }

    [Fact]
    public async Task TranslateAsync_EmptyText_ReturnsFailure()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var translator = CreateTranslator(handlerMock.Object);

        // Act
        var result = await translator.TranslateAsync("", "cs");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("empty", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TranslateAsync_EmptyApiKey_ReturnsFailure()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var translator = CreateTranslator(handlerMock.Object, apiKey: "");

        // Act
        var result = await translator.TranslateAsync("Hello", "cs");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("API key", result.Error);
    }

    [Fact]
    public async Task TranslateAsync_SuccessfulResponse_ReturnsTranslation()
    {
        // Arrange
        var responseJson = """
        {
            "translations": [
                {
                    "detected_source_language": "EN",
                    "text": "Ahoj"
                }
            ]
        }
        """;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            });

        var translator = CreateTranslator(handlerMock.Object);

        // Act
        var result = await translator.TranslateAsync("Hello", "cs");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Ahoj", result.Translation);
        Assert.Equal("DeepL", result.Provider);
        Assert.Equal("en", result.DetectedSourceLanguage);
    }

    [Fact]
    public async Task TranslateAsync_ApiError_ReturnsFailure()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("Invalid API key")
            });

        var translator = CreateTranslator(handlerMock.Object);

        // Act
        var result = await translator.TranslateAsync("Hello", "cs");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Unauthorized", result.Error);
        Assert.Equal("DeepL", result.Provider);
    }
}
