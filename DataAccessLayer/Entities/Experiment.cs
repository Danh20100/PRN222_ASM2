using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

/// <summary>
/// A benchmark experiment run combining a specific RAG configuration.
/// Status: Pending | Running | Completed | Failed
/// </summary>
public class Experiment
{
    [Key]
    public int ExperimentId { get; set; }

    [ForeignKey(nameof(Subject))]
    public int SubjectId { get; set; }

    [ForeignKey(nameof(EmbeddingModel))]
    public int EmbeddingModelId { get; set; }

    [ForeignKey(nameof(AiModel))]
    public int AiModelId { get; set; }

    [ForeignKey(nameof(ChunkingStrategy))]
    public int ChunkingStrategyId { get; set; }

    [Required, MaxLength(200)]
    public string ExperimentName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending | Running | Completed | Failed

    public int TopK { get; set; } = 3;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    // Navigation
    public Subject Subject { get; set; } = null!;
    public EmbeddingModel EmbeddingModel { get; set; } = null!;
    public AiModel AiModel { get; set; } = null!;
    public ChunkingStrategy ChunkingStrategy { get; set; } = null!;
    public ICollection<TestSet> TestSets { get; set; } = [];
    public ICollection<BenchmarkResult> BenchmarkResults { get; set; } = [];
}
