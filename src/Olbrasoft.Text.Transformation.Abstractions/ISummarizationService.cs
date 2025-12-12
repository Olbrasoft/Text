namespace Olbrasoft.Text.Transformation.Abstractions;

/// <summary>
/// Service for AI-powered summarization of text content.
/// </summary>
public interface ISummarizationService
{
    /// <summary>
    /// Summarizes the given text content.
    /// </summary>
    /// <param name="content">Text to summarize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary result with status information.</returns>
    Task<SummarizationResult> SummarizeAsync(string content, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a summarization request.
/// </summary>
public class SummarizationResult
{
    /// <summary>Whether summarization was successful.</summary>
    public bool Success { get; set; }

    /// <summary>The generated summary, or null if failed.</summary>
    public string? Summary { get; set; }

    /// <summary>Error message if summarization failed.</summary>
    public string? Error { get; set; }

    /// <summary>Which provider was used (for logging).</summary>
    public string? Provider { get; set; }

    /// <summary>Which model was used (for logging).</summary>
    public string? Model { get; set; }

    /// <summary>Creates a successful result.</summary>
    public static SummarizationResult Ok(string summary, string provider, string model) =>
        new() { Success = true, Summary = summary, Provider = provider, Model = model };

    /// <summary>Creates a failed result.</summary>
    public static SummarizationResult Fail(string error) =>
        new() { Success = false, Error = error };
}
