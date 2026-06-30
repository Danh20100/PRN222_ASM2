using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities;

/// <summary>
/// Supported providers: Gemini | HuggingFace | OpenAI | Ollama
/// </summary>
public class EmbeddingModel
{
    /// <summary>
    /// The unique identifier of the embedding model.
    /// </summary>
    [Key]
    public int EmbeddingModelId { get; set; }

    /// <summary>
    /// The technical name of the embedding model.
    /// </summary>
    [Required, MaxLength(100)]
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// The provider of the embedding model (e.g., Gemini, OpenAI, HuggingFace, Ollama).
    /// </summary>
    [Required, MaxLength(50)]
    public string Provider { get; set; } = string.Empty; // Gemini | HuggingFace | OpenAI | Ollama

    [MaxLength(300)]
    public string? ApiEndpoint { get; set; }

    public int VectorDimension { get; set; } = 768;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsDefault { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<DocumentIndex> DocumentIndexes { get; set; } = [];
    public ICollection<DocumentChunk> DocumentChunks { get; set; } = [];
}
