using Olbrasoft.Text.Transformation.Abstractions;

namespace Olbrasoft.Text.Transformation.Abstractions.Tests;

public class EmbeddingSettingsTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var settings = new EmbeddingSettings();

        Assert.Equal(EmbeddingProvider.Cohere, settings.Provider);
        Assert.Equal(1024, settings.Dimensions);
        Assert.Equal("embed-multilingual-v3.0", settings.Model);
        Assert.Empty(settings.CohereApiKeys);
        Assert.Null(settings.CohereApiKey);
        Assert.Equal("embed-multilingual-v3.0", settings.CohereModel);
    }

    [Fact]
    public void GetCohereApiKeys_WithEmptyArrayAndNullLegacy_ReturnsEmpty()
    {
        var settings = new EmbeddingSettings
        {
            CohereApiKeys = [],
            CohereApiKey = null
        };

        var keys = settings.GetCohereApiKeys();

        Assert.Empty(keys);
    }

    [Fact]
    public void GetCohereApiKeys_WithArrayKeys_ReturnsArrayKeys()
    {
        var settings = new EmbeddingSettings
        {
            CohereApiKeys = ["key1", "key2", "key3"]
        };

        var keys = settings.GetCohereApiKeys();

        Assert.Equal(3, keys.Count);
        Assert.Equal("key1", keys[0]);
        Assert.Equal("key2", keys[1]);
        Assert.Equal("key3", keys[2]);
    }

    [Fact]
    public void GetCohereApiKeys_WithLegacyKeyOnly_ReturnsLegacyKey()
    {
        var settings = new EmbeddingSettings
        {
            CohereApiKeys = [],
            CohereApiKey = "legacy-key"
        };

        var keys = settings.GetCohereApiKeys();

        Assert.Single(keys);
        Assert.Equal("legacy-key", keys[0]);
    }

    [Fact]
    public void GetCohereApiKeys_WithBothArrayAndLegacy_PrefersArray()
    {
        var settings = new EmbeddingSettings
        {
            CohereApiKeys = ["array-key"],
            CohereApiKey = "legacy-key"
        };

        var keys = settings.GetCohereApiKeys();

        Assert.Single(keys);
        Assert.Equal("array-key", keys[0]);
    }

    [Fact]
    public void GetCohereApiKeys_FiltersOutEmptyAndWhitespaceKeys()
    {
        var settings = new EmbeddingSettings
        {
            CohereApiKeys = ["key1", "", "  ", "key2", null!]
        };

        var keys = settings.GetCohereApiKeys();

        Assert.Equal(2, keys.Count);
        Assert.Equal("key1", keys[0]);
        Assert.Equal("key2", keys[1]);
    }

    [Fact]
    public void GetCohereApiKeys_WithOnlyWhitespaceInArray_FallsBackToLegacy()
    {
        var settings = new EmbeddingSettings
        {
            CohereApiKeys = ["", "  "],
            CohereApiKey = "legacy-key"
        };

        var keys = settings.GetCohereApiKeys();

        Assert.Single(keys);
        Assert.Equal("legacy-key", keys[0]);
    }

    [Fact]
    public void CohereModel_CanBeSet()
    {
        var settings = new EmbeddingSettings();

        settings.CohereModel = "custom-model";

        Assert.Equal("custom-model", settings.CohereModel);
        Assert.Equal("custom-model", settings.Cohere.Model);
    }

    [Fact]
    public void Model_IsIndependentFromCohereModel()
    {
        var settings = new EmbeddingSettings();

        settings.Model = "custom-model";

        Assert.Equal("custom-model", settings.Model);
        // CohereModel is separate (delegates to Cohere.Model)
        Assert.Equal("embed-multilingual-v3.0", settings.CohereModel);
    }
}
