namespace Olbrasoft.Text.Translation;

/// <summary>
/// Service for splitting long texts into chunks and reassembling them.
/// </summary>
public interface ITextChunker
{
    /// <summary>
    /// Splits text into chunks that don't exceed the maximum chunk size.
    /// Attempts to preserve sentence boundaries for better translation quality.
    /// </summary>
    /// <param name="text">Text to split into chunks.</param>
    /// <param name="maxChunkSize">Maximum size of each chunk in characters.</param>
    /// <returns>List of text chunks, each not exceeding maxChunkSize.</returns>
    IReadOnlyList<string> ChunkText(string text, int maxChunkSize);

    /// <summary>
    /// Joins translated chunks back into a single text.
    /// </summary>
    /// <param name="translatedChunks">List of translated text chunks.</param>
    /// <returns>Complete translated text.</returns>
    string JoinChunks(IReadOnlyList<string> translatedChunks);
}
