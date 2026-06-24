namespace BusinessLayer.Interfaces;

/// <summary>
/// Contract for all embedding providers.
/// Implementations: Gemini, HuggingFace, OpenAI, Ollama
/// </summary>
public interface IEmbeddingProvider
{
    string ProviderName { get; }

    /// <summary>
    /// Embed a single text into a float vector.
    /// Handles 429 Rate Limit with retry internally.
    /// </summary>
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch embed multiple texts. Returns list in same order.
    /// </summary>
    Task<List<float[]>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}
