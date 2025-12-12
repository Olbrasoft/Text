using Olbrasoft.Text.Transformation.Abstractions;

namespace Olbrasoft.Text.Transformation.Abstractions.Tests;

public class EmbeddingProviderTests
{
    [Fact]
    public void Cohere_HasValue0()
    {
        Assert.Equal(0, (int)EmbeddingProvider.Cohere);
    }

    [Fact]
    public void Enum_HasOneValue()
    {
        var values = Enum.GetValues<EmbeddingProvider>();
        Assert.Single(values);
    }

    [Fact]
    public void Parse_Cohere_ReturnsCorrectValue()
    {
        var result = Enum.Parse<EmbeddingProvider>("Cohere");
        Assert.Equal(EmbeddingProvider.Cohere, result);
    }
}
