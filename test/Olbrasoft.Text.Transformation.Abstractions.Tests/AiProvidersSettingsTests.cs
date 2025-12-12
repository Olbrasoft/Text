using Olbrasoft.Text.Transformation.Abstractions;

namespace Olbrasoft.Text.Transformation.Abstractions.Tests;

public class AiProvidersSettingsTests
{
    [Fact]
    public void DefaultValues_ShouldCreateEmptyProviders()
    {
        var settings = new AiProvidersSettings();

        Assert.NotNull(settings.Cerebras);
        Assert.NotNull(settings.Groq);
        Assert.NotNull(settings.Cohere);
    }

    [Fact]
    public void Cerebras_CanBeConfigured()
    {
        var settings = new AiProvidersSettings
        {
            Cerebras = new AiProviderConfig
            {
                Endpoint = "https://cerebras.example.com/",
                Models = ["model1", "model2"],
                Keys = ["key1", "key2"]
            }
        };

        Assert.Equal("https://cerebras.example.com/", settings.Cerebras.Endpoint);
        Assert.Equal(2, settings.Cerebras.Models.Length);
        Assert.Equal(2, settings.Cerebras.Keys.Length);
    }

    [Fact]
    public void Groq_CanBeConfigured()
    {
        var settings = new AiProvidersSettings
        {
            Groq = new AiProviderConfig
            {
                Endpoint = "https://groq.example.com/",
                Models = ["llama-70b"],
                Keys = ["groq-key"]
            }
        };

        Assert.Equal("https://groq.example.com/", settings.Groq.Endpoint);
        Assert.Single(settings.Groq.Models);
        Assert.Single(settings.Groq.Keys);
    }

    [Fact]
    public void Cohere_HasCorrectDefaults()
    {
        var settings = new AiProvidersSettings();

        Assert.Equal("https://api.cohere.com/v2/", settings.Cohere.Endpoint);
        Assert.Contains("command-a-translate-08-2025", settings.Cohere.TranslationModels);
        Assert.Empty(settings.Cohere.Keys);
    }
}

public class AiProviderConfigTests
{
    [Fact]
    public void DefaultValues_ShouldBeEmpty()
    {
        var config = new AiProviderConfig();

        Assert.Equal(string.Empty, config.Endpoint);
        Assert.Empty(config.Models);
        Assert.Empty(config.Keys);
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        var config = new AiProviderConfig
        {
            Endpoint = "https://api.example.com/",
            Models = ["model-a", "model-b"],
            Keys = ["key-1", "key-2", "key-3"]
        };

        Assert.Equal("https://api.example.com/", config.Endpoint);
        Assert.Equal(2, config.Models.Length);
        Assert.Equal(3, config.Keys.Length);
    }
}

public class CohereProviderConfigTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var config = new CohereProviderConfig();

        Assert.Equal("https://api.cohere.com/v2/", config.Endpoint);
        Assert.Equal(2, config.TranslationModels.Length);
        Assert.Empty(config.Keys);
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        var config = new CohereProviderConfig
        {
            Endpoint = "https://custom.cohere.com/",
            TranslationModels = ["custom-model"],
            Keys = ["cohere-key-1", "cohere-key-2"]
        };

        Assert.Equal("https://custom.cohere.com/", config.Endpoint);
        Assert.Single(config.TranslationModels);
        Assert.Equal(2, config.Keys.Length);
    }
}
