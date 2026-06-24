using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

/// <summary>
/// Status values: Pending | Processing | Indexed | Failed
/// </summary>
public class Document
{
    [Key]
    public int DocumentId { get; set; }

    [ForeignKey(nameof(Chapter))]
    public int ChapterId { get; set; }

    [Required, MaxLength(300)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? OriginalFileName { get; set; }

    [MaxLength(50)]
    public string? FileType { get; set; } // pdf | docx | txt | md

    public long FileSizeBytes { get; set; }

    [MaxLength(500)]
    public string? StoragePath { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending | Processing | Indexed | Failed

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    public int? TotalChunks { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public DateTime? IndexedAt { get; set; }

    public int UploadedByUserId { get; set; }

    [ForeignKey(nameof(UploadedByUserId))]
    public User UploadedBy { get; set; } = null!;

    // Navigation
    public Chapter Chapter { get; set; } = null!;
    public ICollection<DocumentIndex> DocumentIndexes { get; set; } = [];
    public ICollection<DocumentChunk> DocumentChunks { get; set; } = [];
}
