using Olbrasoft.Text.Transformation.Abstractions;

namespace Olbrasoft.Text.Transformation.Abstractions.Tests;

public class TranslationSettingsTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var settings = new TranslationSettings();

        Assert.Equal(300, settings.MaxTokens);
        Assert.Equal(0.2, settings.Temperature);
        Assert.Equal("Czech", settings.TargetLanguage);
        Assert.NotNull(settings.SystemPrompt);
        Assert.Contains("Czech", settings.SystemPrompt);
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        var settings = new TranslationSettings
        {
            MaxTokens = 500,
            Temperature = 0.3,
            TargetLanguage = "German",
            SystemPrompt = "Translate to German"
        };

        Assert.Equal(500, settings.MaxTokens);
        Assert.Equal(0.3, settings.Temperature);
        Assert.Equal("German", settings.TargetLanguage);
        Assert.Equal("Translate to German", settings.SystemPrompt);
    }
}
