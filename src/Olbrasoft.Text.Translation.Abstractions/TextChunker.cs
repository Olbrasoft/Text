using System.Text;

namespace Olbrasoft.Text.Translation;

/// <summary>
/// Implementation of text chunking service that splits long texts into smaller pieces
/// while preserving sentence boundaries for better translation quality.
/// </summary>
public class TextChunker : ITextChunker
{
    private static readonly char[] SentenceTerminators = ['.', '!', '?', '\n'];
    private const double SafetyMargin = 0.9; // Use 90% of max size to be safe

    /// <summary>
    /// Splits text into chunks that don't exceed the maximum chunk size.
    /// Strategy:
    /// 1. Try to split by sentences (at . ! ? or newline)
    /// 2. If sentence is too long, split by words
    /// 3. If word is too long, split by characters (edge case)
    /// Uses 90% of maxChunkSize as safety margin.
    /// </summary>
    public IReadOnlyList<string> ChunkText(string text, int maxChunkSize)
    {
        if (string.IsNullOrEmpty(text))
            return Array.Empty<string>();

        if (text.Length <= maxChunkSize)
            return new[] { text };

        var safeChunkSize = (int)(maxChunkSize * SafetyMargin);
        var chunks = new List<string>();
        var currentChunk = new StringBuilder();

        // Split by sentences
        var sentences = SplitIntoSentences(text);

        foreach (var sentence in sentences)
        {
            // If single sentence exceeds safe chunk size, split it further
            if (sentence.Length > safeChunkSize)
            {
                // Save current chunk if not empty
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }

                // Split long sentence by words
                chunks.AddRange(SplitLongText(sentence, safeChunkSize));
                continue;
            }

            // Check if adding this sentence would exceed limit
            if (currentChunk.Length + sentence.Length > safeChunkSize)
            {
                // Save current chunk and start new one
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }
            }

            currentChunk.Append(sentence);
        }

        // Add remaining text
        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }

    /// <summary>
    /// Joins translated chunks back into complete text with proper spacing.
    /// </summary>
    public string JoinChunks(IReadOnlyList<string> translatedChunks)
    {
        if (translatedChunks == null || translatedChunks.Count == 0)
            return string.Empty;

        // Join chunks with spaces between them
        var result = new StringBuilder();
        for (int i = 0; i < translatedChunks.Count; i++)
        {
            var chunk = translatedChunks[i].Trim();
            if (string.IsNullOrEmpty(chunk))
                continue;

            // Add space before chunk if not first chunk
            if (result.Length > 0)
            {
                result.Append(' ');
            }

            result.Append(chunk);
        }

        return result.ToString();
    }

    private static List<string> SplitIntoSentences(string text)
    {
        var sentences = new List<string>();
        var currentSentence = new StringBuilder();

        for (int i = 0; i < text.Length; i++)
        {
            currentSentence.Append(text[i]);

            if (Array.IndexOf(SentenceTerminators, text[i]) >= 0)
            {
                // Check if next char is whitespace or end of text
                if (i + 1 >= text.Length || char.IsWhiteSpace(text[i + 1]))
                {
                    sentences.Add(currentSentence.ToString());
                    currentSentence.Clear();
                }
            }
        }

        // Add remaining text
        if (currentSentence.Length > 0)
        {
            sentences.Add(currentSentence.ToString());
        }

        return sentences;
    }

    private static List<string> SplitLongText(string text, int maxSize)
    {
        var chunks = new List<string>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new StringBuilder();

        foreach (var word in words)
        {
            // If single word exceeds limit, split by characters (edge case)
            if (word.Length > maxSize)
            {
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }

                chunks.AddRange(SplitByCharacters(word, maxSize));
                continue;
            }

            // Check if adding this word would exceed limit
            var potentialLength = currentChunk.Length + (currentChunk.Length > 0 ? 1 : 0) + word.Length;
            if (potentialLength > maxSize)
            {
                chunks.Add(currentChunk.ToString().Trim());
                currentChunk.Clear();
            }

            if (currentChunk.Length > 0)
                currentChunk.Append(' ');

            currentChunk.Append(word);
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }

    private static List<string> SplitByCharacters(string text, int maxSize)
    {
        var chunks = new List<string>();
        for (int i = 0; i < text.Length; i += maxSize)
        {
            var remainingLength = text.Length - i;
            var chunkLength = Math.Min(maxSize, remainingLength);
            chunks.Add(text.Substring(i, chunkLength));
        }
        return chunks;
    }

    private static bool EndsWithPunctuation(StringBuilder text)
    {
        if (text.Length == 0)
            return false;

        var lastChar = text[text.Length - 1];
        return Array.IndexOf(SentenceTerminators, lastChar) >= 0 ||
               char.IsPunctuation(lastChar);
    }
}
