using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

/// <summary>
/// Tracks WHICH EmbeddingModel was used to index a Document.
/// A document can be re-indexed with different models.
/// </summary>
public class DocumentIndex
{
    [Key]
    public int DocumentIndexId { get; set; }

    [ForeignKey(nameof(Document))]
    public int DocumentId { get; set; }

    [ForeignKey(nameof(EmbeddingModel))]
    public int EmbeddingModelId { get; set; }

    [ForeignKey(nameof(ChunkingStrategy))]
    public int ChunkingStrategyId { get; set; }

    public int TotalChunks { get; set; }

    public int VectorDimension { get; set; }

    public DateTime IndexedAt { get; set; } = DateTime.UtcNow;

    public double? IndexingDurationSeconds { get; set; }

    // Navigation
    public Document Document { get; set; } = null!;
    public EmbeddingModel EmbeddingModel { get; set; } = null!;
    public ChunkingStrategy ChunkingStrategy { get; set; } = null!;
}
