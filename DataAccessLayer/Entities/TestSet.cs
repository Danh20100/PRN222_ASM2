using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

/// <summary>
/// A question-answer pair used for RAG evaluation.
/// </summary>
public class TestSet
{
    [Key]
    public int TestSetId { get; set; }

    [ForeignKey(nameof(Experiment))]
    public int ExperimentId { get; set; }

    [Required]
    public string Question { get; set; } = string.Empty;

    [Required]
    public string ExpectedAnswer { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Tags { get; set; } // comma-separated tags for grouping

    public int OrderIndex { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Experiment Experiment { get; set; } = null!;
    public ICollection<BenchmarkResult> BenchmarkResults { get; set; } = [];
}
