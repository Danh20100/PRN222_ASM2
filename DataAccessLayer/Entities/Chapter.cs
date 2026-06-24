using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

public class Chapter
{
    [Key]
    public int ChapterId { get; set; }

    [ForeignKey(nameof(Subject))]
    public int SubjectId { get; set; }

    [Required, MaxLength(200)]
    public string ChapterName { get; set; } = string.Empty;

    public int OrderIndex { get; set; } = 0;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Subject Subject { get; set; } = null!;
    public ICollection<Document> Documents { get; set; } = [];
}
