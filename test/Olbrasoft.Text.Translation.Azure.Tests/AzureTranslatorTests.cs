using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace Olbrasoft.Text.Translation.Azure.Tests;

public class AzureTranslatorTests
{
    private readonly Mock<ILogger<AzureTranslator>> _loggerMock = new();

    private AzureTranslator CreateTranslator(
        HttpMessageHandler handler,
        string apiKey = "test-key",
        string region = "westeurope",
        string endpoint = "https://api.cognitive.microsofttranslator.com/")
    {
        var settings = Options.Create(new AzureTranslatorSettings
        {
            ApiKey = apiKey,
            Region = region,
            Endpoint = endpoint
        });

        var httpClient = new HttpClient(handler);
        return new AzureTranslator(httpClient, settings, _loggerMock.Object);
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
        [
            {
                "detectedLanguage": {
                    "language": "en",
                    "score": 1.0
                },
                "translations": [
                    {
                        "text": "Ahoj",
                        "to": "cs"
                    }
                ]
            }
        ]
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
        Assert.Equal("Azure", result.Provider);
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
                Content = new StringContent("Invalid subscription key")
            });

        var translator = CreateTranslator(handlerMock.Object);

        // Act
        var result = await translator.TranslateAsync("Hello", "cs");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Unauthorized", result.Error);
        Assert.Equal("Azure", result.Provider);
    }
}
