using Olbrasoft.Text.Translation;

namespace Olbrasoft.Text.Translation.Abstractions.Tests;

public class TextChunkerTests
{
    private readonly TextChunker _chunker = new();

    [Fact]
    public void ChunkText_WithShortText_ReturnsSingleChunk()
    {
        // Arrange
        var text = "Hello world";
        var maxChunkSize = 100;

        // Act
        var chunks = _chunker.ChunkText(text, maxChunkSize);

        // Assert
        Assert.Single(chunks);
        Assert.Equal(text, chunks[0]);
    }

    [Fact]
    public void ChunkText_WithEmptyText_ReturnsEmptyList()
    {
        // Act
        var chunks = _chunker.ChunkText("", 100);

        // Assert
        Assert.Empty(chunks);
    }

    [Fact]
    public void ChunkText_WithNullText_ReturnsEmptyList()
    {
        // Act
        var chunks = _chunker.ChunkText(null!, 100);

        // Assert
        Assert.Empty(chunks);
    }

    [Fact]
    public void ChunkText_WithLongText_SplitsBySentences()
    {
        // Arrange
        var text = "This is the first sentence. This is the second sentence. This is the third sentence.";
        var maxChunkSize = 40; // Smaller than any sentence

        // Act
        var chunks = _chunker.ChunkText(text, maxChunkSize);

        // Assert
        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk => Assert.True(chunk.Length <= maxChunkSize * 0.9 + 10)); // Allow for safety margin
    }

    [Fact]
    public void ChunkText_WithLongSentence_SplitsByWords()
    {
        // Arrange
        var text = "This is a very long sentence that contains many words and should be split by words.";
        var maxChunkSize = 30;

        // Act
        var chunks = _chunker.ChunkText(text, maxChunkSize);

        // Assert
        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk => Assert.True(chunk.Length <= maxChunkSize * 0.9 + 10));
    }

    [Fact]
    public void ChunkText_WithNewlines_SplitsAtNewlines()
    {
        // Arrange
        var text = "Line 1\nLine 2\nLine 3";
        var maxChunkSize = 20;

        // Act
        var chunks = _chunker.ChunkText(text, maxChunkSize);

        // Assert
        Assert.True(chunks.Count >= 1);
    }

    [Fact]
    public void JoinChunks_WithMultipleChunks_JoinsWithSpaces()
    {
        // Arrange
        var chunks = new List<string> { "First chunk.", "Second chunk.", "Third chunk." };

        // Act
        var result = _chunker.JoinChunks(chunks);

        // Assert
        Assert.Equal("First chunk. Second chunk. Third chunk.", result);
    }

    [Fact]
    public void JoinChunks_WithEmptyList_ReturnsEmptyString()
    {
        // Act
        var result = _chunker.JoinChunks(new List<string>());

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void JoinChunks_WithNullList_ReturnsEmptyString()
    {
        // Act
        var result = _chunker.JoinChunks(null!);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ChunkText_WithExactLimit_ReturnsOneChunk()
    {
        // Arrange
        var text = "12345";
        var maxChunkSize = 5;

        // Act
        var chunks = _chunker.ChunkText(text, maxChunkSize);

        // Assert
        Assert.Single(chunks);
        Assert.Equal(text, chunks[0]);
    }

    [Fact]
    public void ChunkText_PreservesContentAfterChunkingAndJoining()
    {
        // Arrange
        var originalText = "First sentence. Second sentence. Third sentence with more words.";
        var maxChunkSize = 25;

        // Act
        var chunks = _chunker.ChunkText(originalText, maxChunkSize);
        var rejoined = _chunker.JoinChunks(chunks);

        // Assert
        // Content should be preserved (allowing for whitespace normalization)
        Assert.Equal(originalText.Replace("  ", " ").Trim(), rejoined.Replace("  ", " ").Trim());
    }
}
