using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Olbrasoft.Text.Translation.Bing.Tests;

public class BingFreeTranslatorTests
{
    private readonly Mock<ILogger<BingFreeTranslator>> _loggerMock = new();

    private BingFreeTranslator CreateTranslator(
        int timeoutSeconds = 10,
        string? userAgent = null)
    {
        var settings = Options.Create(new BingFreeTranslatorSettings
        {
            TimeoutSeconds = timeoutSeconds,
            UserAgent = userAgent
        });

        return new BingFreeTranslator(settings, _loggerMock.Object);
    }

    [Fact]
    public async Task TranslateAsync_EmptyText_ReturnsFailure()
    {
        // Arrange
        var translator = CreateTranslator();

        // Act
        var result = await translator.TranslateAsync("", "cs");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("empty", result.Error!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Bing", result.Provider);
    }

    [Fact]
    public async Task TranslateAsync_WhitespaceText_ReturnsFailure()
    {
        // Arrange
        var translator = CreateTranslator();

        // Act
        var result = await translator.TranslateAsync("   ", "cs");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("empty", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(Skip = "Integration test - requires network access. Run manually.")]
    public async Task TranslateAsync_EnglishToCzech_ReturnsTranslation()
    {
        // Arrange
        var translator = CreateTranslator();

        // Act
        var result = await translator.TranslateAsync("Hello, how are you?", "cs", sourceLanguage: "en");

        // Assert
        Assert.True(result.Success, $"Translation failed: {result.Error}");
        Assert.NotNull(result.Translation);
        Assert.NotEmpty(result.Translation);
        Assert.Equal("Bing", result.Provider);
        Assert.Equal("en", result.DetectedSourceLanguage);

        // Bing should translate "Hello, how are you?" to something like "Ahoj, jak se máš?"
        Assert.Contains("ahoj", result.Translation.ToLowerInvariant());
    }

    [Fact(Skip = "Integration test - requires network access. Run manually.")]
    public async Task TranslateAsync_AutoDetectLanguage_ReturnsTranslation()
    {
        // Arrange
        var translator = CreateTranslator();

        // Act
        var result = await translator.TranslateAsync("Hello", "cs");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Translation);
        Assert.Equal("Bing", result.Provider);
        Assert.NotNull(result.DetectedSourceLanguage);
        Assert.Equal("en", result.DetectedSourceLanguage);
    }

    [Fact(Skip = "Integration test - tests timeout behavior. Run manually.")]
    public async Task TranslateAsync_ShortTimeout_MayTimeout()
    {
        // Arrange
        var translator = CreateTranslator(timeoutSeconds: 1);

        // Act
        var result = await translator.TranslateAsync("This is a very long text that might take time to translate...", "cs");

        // Assert
        // This test may pass or fail depending on network speed
        // We just verify it handles timeout gracefully
        if (!result.Success)
        {
            Assert.Contains("timeout", result.Error!, StringComparison.OrdinalIgnoreCase);
        }
    }
}
