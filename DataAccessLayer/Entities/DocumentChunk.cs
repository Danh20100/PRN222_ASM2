using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

/// <summary>
/// Stores a text chunk along with its embedding vector serialized as JSON float[].
/// Example EmbeddingJson: "[0.123, -0.456, 0.789, ...]"
/// </summary>
public class DocumentChunk
{
    [Key]
    public int DocumentChunkId { get; set; }

    [ForeignKey(nameof(Document))]
    public int DocumentId { get; set; }

    [ForeignKey(nameof(EmbeddingModel))]
    public int EmbeddingModelId { get; set; }

    public int ChunkIndex { get; set; }

    [Required]
    public string ChunkText { get; set; } = string.Empty;

    public int TokenCount { get; set; }

    /// <summary>float[] serialized as JSON string — e.g. "[0.1, 0.2, ...]"</summary>
    [Required]
    public string EmbeddingJson { get; set; } = "[]";

    public int VectorDimension { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Document Document { get; set; } = null!;
    public EmbeddingModel EmbeddingModel { get; set; } = null!;
    public ICollection<ChatCitation> Citations { get; set; } = [];
}
