using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

/// <summary>
/// Stores the RAG evaluation scores for a single TestSet question.
/// Faithfulness: how faithful the answer is to retrieved context.
/// Relevance: how relevant the retrieved context is to the question.
/// </summary>
public class BenchmarkResult
{
    [Key]
    public int BenchmarkResultId { get; set; }

    [ForeignKey(nameof(Experiment))]
    public int ExperimentId { get; set; }

    [ForeignKey(nameof(TestSet))]
    public int TestSetId { get; set; }

    public string? GeneratedAnswer { get; set; }

    /// <summary>JSON array of retrieved chunk IDs: "[1, 2, 3]"</summary>
    public string? RetrievedChunkIds { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal? FaithfulnessScore { get; set; }  // 0.0 - 1.0

    [Column(TypeName = "decimal(5,4)")]
    public decimal? RelevanceScore { get; set; }       // 0.0 - 1.0

    [Column(TypeName = "decimal(5,4)")]
    public decimal? ContextRecallScore { get; set; }   // 0.0 - 1.0

    [Column(TypeName = "decimal(5,4)")]
    public decimal? AnswerSimilarityScore { get; set; } // 0.0 - 1.0

    public int? LatencyMs { get; set; }

    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    // Navigation
    public Experiment Experiment { get; set; } = null!;
    public TestSet TestSet { get; set; } = null!;
}
