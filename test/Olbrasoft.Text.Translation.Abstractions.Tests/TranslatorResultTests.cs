using Olbrasoft.Text.Translation;

namespace Olbrasoft.Text.Translation.Abstractions.Tests;

public class TranslatorResultTests
{
    [Fact]
    public void Ok_ShouldCreateSuccessResult()
    {
        // Act
        var result = TranslatorResult.Ok("Přeložený text", "DeepL", "en");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Přeložený text", result.Translation);
        Assert.Equal("DeepL", result.Provider);
        Assert.Equal("en", result.DetectedSourceLanguage);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Ok_WithoutDetectedLanguage_ShouldCreateSuccessResult()
    {
        // Act
        var result = TranslatorResult.Ok("Translated", "Azure");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Translated", result.Translation);
        Assert.Equal("Azure", result.Provider);
        Assert.Null(result.DetectedSourceLanguage);
    }

    [Fact]
    public void Fail_ShouldCreateFailedResult()
    {
        // Act
        var result = TranslatorResult.Fail("API error", "DeepL");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Translation);
        Assert.Equal("API error", result.Error);
        Assert.Equal("DeepL", result.Provider);
    }

    [Fact]
    public void Fail_WithoutProvider_ShouldCreateFailedResult()
    {
        // Act
        var result = TranslatorResult.Fail("Network error");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Network error", result.Error);
        Assert.Null(result.Provider);
    }
}
