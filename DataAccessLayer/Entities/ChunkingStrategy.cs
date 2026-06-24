using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities;

/// <summary>
/// Chunking strategy config. StrategyType: FixedSize | Paragraph | Sentence | Recursive
/// </summary>
public class ChunkingStrategy
{
    [Key]
    public int ChunkingStrategyId { get; set; }

    [Required, MaxLength(100)]
    public string StrategyName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string StrategyType { get; set; } = "FixedSize"; // FixedSize | Paragraph | Sentence | Recursive

    public int ChunkSize { get; set; } = 512;

    public int ChunkOverlap { get; set; } = 64;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsDefault { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<DocumentIndex> DocumentIndexes { get; set; } = [];
    public ICollection<Experiment> Experiments { get; set; } = [];
}
