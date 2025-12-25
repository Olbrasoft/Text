using Microsoft.Extensions.Logging;
using Moq;
using Olbrasoft.Text.Translation;

namespace Olbrasoft.Text.Translation.Abstractions.Tests;

public class ChunkedTranslationServiceTests
{
    private readonly Mock<ITextChunker> _mockChunker;
    private readonly Mock<ITranslator> _mockTranslator;
    private readonly Mock<ILogger<ChunkedTranslationService>> _mockLogger;
    private readonly ChunkedTranslationService _service;

    public ChunkedTranslationServiceTests()
    {
        _mockChunker = new Mock<ITextChunker>();
        _mockTranslator = new Mock<ITranslator>();
        _mockLogger = new Mock<ILogger<ChunkedTranslationService>>();
        _service = new ChunkedTranslationService(_mockChunker.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task TranslateAsync_WithShortText_CallsTranslatorDirectly()
    {
        // Arrange
        var text = "Hello";
        _mockTranslator.Setup(t => t.MaxRequestCharacters).Returns(1000);
        _mockTranslator
            .Setup(t => t.TranslateAsync(text, "cs", null, default))
            .ReturnsAsync(TranslatorResult.Ok("Ahoj", "Google"));

        // Act
        var result = await _service.TranslateAsync(_mockTranslator.Object, text, "cs");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Ahoj", result.Translation);
        _mockTranslator.Verify(t => t.TranslateAsync(text, "cs", null, default), Times.Once);
        _mockChunker.Verify(c => c.ChunkText(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task TranslateAsync_WithLongText_ChunksAndTranslates()
    {
        // Arrange
        var longText = new string('a', 10000);
        var chunks = new List<string> { "chunk1", "chunk2", "chunk3" };

        _mockTranslator.Setup(t => t.MaxRequestCharacters).Returns(5000);
        _mockChunker
            .Setup(c => c.ChunkText(longText, 5000))
            .Returns(chunks);

        _mockTranslator
            .Setup(t => t.TranslateAsync("chunk1", "cs", null, default))
            .ReturnsAsync(TranslatorResult.Ok("přeložený1", "Google"));
        _mockTranslator
            .Setup(t => t.TranslateAsync("chunk2", "cs", null, default))
            .ReturnsAsync(TranslatorResult.Ok("přeložený2", "Google"));
        _mockTranslator
            .Setup(t => t.TranslateAsync("chunk3", "cs", null, default))
            .ReturnsAsync(TranslatorResult.Ok("přeložený3", "Google"));

        _mockChunker
            .Setup(c => c.JoinChunks(It.IsAny<IReadOnlyList<string>>()))
            .Returns("přeložený1 přeložený2 přeložený3");

        // Act
        var result = await _service.TranslateAsync(_mockTranslator.Object, longText, "cs");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("přeložený1 přeložený2 přeložený3", result.Translation);
        _mockChunker.Verify(c => c.ChunkText(longText, 5000), Times.Once);
        _mockTranslator.Verify(t => t.TranslateAsync(It.IsAny<string>(), "cs", null, default), Times.Exactly(3));
        _mockChunker.Verify(c => c.JoinChunks(It.IsAny<IReadOnlyList<string>>()), Times.Once);
    }

    [Fact]
    public async Task TranslateAsync_WithEmptyText_ReturnsFail()
    {
        // Act
        var result = await _service.TranslateAsync(_mockTranslator.Object, "", "cs");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("empty", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TranslateAsync_WithNullTranslator_ReturnsFail()
    {
        // Act
        var result = await _service.TranslateAsync(null!, "text", "cs");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("null", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TranslateAsync_WhenFirstChunkFails_ReturnsFailure()
    {
        // Arrange
        var longText = new string('a', 10000);
        var chunks = new List<string> { "chunk1", "chunk2" };

        _mockTranslator.Setup(t => t.MaxRequestCharacters).Returns(5000);
        _mockChunker.Setup(c => c.ChunkText(longText, 5000)).Returns(chunks);
        _mockTranslator
            .Setup(t => t.TranslateAsync("chunk1", "cs", null, default))
            .ReturnsAsync(TranslatorResult.Fail("Network error", "Google"));

        // Act
        var result = await _service.TranslateAsync(_mockTranslator.Object, longText, "cs");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Network error", result.Error);
        _mockTranslator.Verify(t => t.TranslateAsync("chunk1", "cs", null, default), Times.Once);
        _mockTranslator.Verify(t => t.TranslateAsync("chunk2", "cs", null, default), Times.Never);
    }

    [Fact]
    public async Task TranslateAsync_WhenSecondChunkFails_ReturnsPartialFailure()
    {
        // Arrange
        var longText = new string('a', 10000);
        var chunks = new List<string> { "chunk1", "chunk2", "chunk3" };

        _mockTranslator.Setup(t => t.MaxRequestCharacters).Returns(5000);
        _mockChunker.Setup(c => c.ChunkText(longText, 5000)).Returns(chunks);

        _mockTranslator
            .Setup(t => t.TranslateAsync("chunk1", "cs", null, default))
            .ReturnsAsync(TranslatorResult.Ok("přeložený1", "Google"));
        _mockTranslator
            .Setup(t => t.TranslateAsync("chunk2", "cs", null, default))
            .ReturnsAsync(TranslatorResult.Fail("Rate limit", "Google"));

        // Act
        var result = await _service.TranslateAsync(_mockTranslator.Object, longText, "cs");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Partial translation failure", result.Error!);
        Assert.Contains("2/3", result.Error!);
        _mockTranslator.Verify(t => t.TranslateAsync("chunk1", "cs", null, default), Times.Once);
        _mockTranslator.Verify(t => t.TranslateAsync("chunk2", "cs", null, default), Times.Once);
        _mockTranslator.Verify(t => t.TranslateAsync("chunk3", "cs", null, default), Times.Never);
    }

    [Fact]
    public async Task TranslateAsync_PreservesDetectedLanguageFromFirstChunk()
    {
        // Arrange
        var longText = new string('a', 10000);
        var chunks = new List<string> { "chunk1", "chunk2" };

        _mockTranslator.Setup(t => t.MaxRequestCharacters).Returns(5000);
        _mockChunker.Setup(c => c.ChunkText(longText, 5000)).Returns(chunks);

        _mockTranslator
            .Setup(t => t.TranslateAsync("chunk1", "cs", null, default))
            .ReturnsAsync(TranslatorResult.Ok("přeložený1", "Google", "en"));
        _mockTranslator
            .Setup(t => t.TranslateAsync("chunk2", "cs", null, default))
            .ReturnsAsync(TranslatorResult.Ok("přeložený2", "Google", "en"));

        _mockChunker
            .Setup(c => c.JoinChunks(It.IsAny<IReadOnlyList<string>>()))
            .Returns("přeložený1 přeložený2");

        // Act
        var result = await _service.TranslateAsync(_mockTranslator.Object, longText, "cs");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("en", result.DetectedSourceLanguage);
    }

    [Theory]
    [InlineData(1000, "Bing")]      // Bing Translator limit
    [InlineData(5000, "Google")]    // Google Translator limit
    [InlineData(50000, "Azure")]    // Azure Translator limit
    [InlineData(131000, "DeepL")]   // DeepL Translator limit
    public async Task TranslateAsync_WithRealChunker_RespectsProviderLimits(int maxChars, string providerName)
    {
        // Arrange - Use REAL TextChunker to test actual chunking
        var realChunker = new TextChunker();
        var realLogger = new Mock<ILogger<ChunkedTranslationService>>().Object;
        var serviceWithRealChunker = new ChunkedTranslationService(realChunker, realLogger);

        // Create text that exceeds the limit (1.5x the limit)
        var textLength = (int)(maxChars * 1.5);
        var longText = string.Join(". ", Enumerable.Repeat("This is a test sentence for chunking", textLength / 40));

        _mockTranslator.Setup(t => t.MaxRequestCharacters).Returns(maxChars);

        // Track which chunks were sent to translator
        var receivedChunks = new List<string>();
        _mockTranslator
            .Setup(t => t.TranslateAsync(It.IsAny<string>(), "cs", null, default))
            .ReturnsAsync((string text, string target, string? source, CancellationToken ct) =>
            {
                receivedChunks.Add(text);
                return TranslatorResult.Ok($"translated-{receivedChunks.Count}", providerName);
            });

        // Act
        var result = await serviceWithRealChunker.TranslateAsync(_mockTranslator.Object, longText, "cs");

        // Assert
        Assert.True(result.Success, $"Translation should succeed for {providerName}, but got error: {result.Error}");

        // Verify text was split into multiple chunks (for text > maxChars)
        if (longText.Length > maxChars)
        {
            Assert.True(receivedChunks.Count >= 2,
                $"Expected at least 2 chunks for {longText.Length} chars with {maxChars} limit ({providerName}), got {receivedChunks.Count}");
        }

        // Verify each chunk respects the limit (90% safety margin)
        var safetyLimit = (int)(maxChars * 0.9);
        Assert.All(receivedChunks, chunk =>
        {
            Assert.True(chunk.Length <= safetyLimit,
                $"{providerName}: Chunk length {chunk.Length} exceeds safety limit of {safetyLimit} chars (90% of {maxChars})");
        });

        // Verify translator was called for each chunk
        _mockTranslator.Verify(
            t => t.TranslateAsync(It.IsAny<string>(), "cs", null, default),
            Times.Exactly(receivedChunks.Count));

        // Verify all chunks were joined back
        Assert.NotNull(result.Translation);
        Assert.Contains("translated-", result.Translation);
    }

    [Fact]
    public async Task TranslateAsync_WithBingLimit_SplitsSmallTextIntoMultipleChunks()
    {
        // Arrange - Bing has smallest limit (1000 chars), so test with 2500 chars
        var realChunker = new TextChunker();
        var realLogger = new Mock<ILogger<ChunkedTranslationService>>().Object;
        var serviceWithRealChunker = new ChunkedTranslationService(realChunker, realLogger);

        var longText = string.Join(". ", Enumerable.Repeat("Short test", 120)); // ~1440 chars

        _mockTranslator.Setup(t => t.MaxRequestCharacters).Returns(1000); // Bing limit

        var receivedChunks = new List<string>();
        _mockTranslator
            .Setup(t => t.TranslateAsync(It.IsAny<string>(), "cs", null, default))
            .ReturnsAsync((string text, string target, string? source, CancellationToken ct) =>
            {
                receivedChunks.Add(text);
                return TranslatorResult.Ok($"chunk-{receivedChunks.Count}", "Bing");
            });

        // Act
        var result = await serviceWithRealChunker.TranslateAsync(_mockTranslator.Object, longText, "cs");

        // Assert
        Assert.True(result.Success);
        Assert.True(receivedChunks.Count >= 2, $"Expected at least 2 chunks for Bing (1000 char limit), got {receivedChunks.Count}");
        Assert.All(receivedChunks, chunk => Assert.True(chunk.Length <= 900)); // 90% of 1000
    }
}
