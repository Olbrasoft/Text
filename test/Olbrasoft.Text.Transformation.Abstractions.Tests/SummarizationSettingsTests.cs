using Olbrasoft.Text.Transformation.Abstractions;

namespace Olbrasoft.Text.Transformation.Abstractions.Tests;

public class SummarizationSettingsTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var settings = new SummarizationSettings();

        Assert.Equal(500, settings.MaxTokens);
        Assert.Equal(0.3, settings.Temperature);
        Assert.NotNull(settings.SystemPrompt);
        Assert.Contains("summarize", settings.SystemPrompt.ToLower());
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        var settings = new SummarizationSettings
        {
            MaxTokens = 1000,
            Temperature = 0.5,
            SystemPrompt = "Custom prompt"
        };

        Assert.Equal(1000, settings.MaxTokens);
        Assert.Equal(0.5, settings.Temperature);
        Assert.Equal("Custom prompt", settings.SystemPrompt);
    }
}
