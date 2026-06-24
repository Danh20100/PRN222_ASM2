using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities;

public class Subject
{
    [Key]
    public int SubjectId { get; set; }

    [Required, MaxLength(20)]
    public string SubjectCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string SubjectName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<SubjectTeacher> SubjectTeachers { get; set; } = [];
    public ICollection<Chapter> Chapters { get; set; } = [];
    public ICollection<ChatSession> ChatSessions { get; set; } = [];
    public ICollection<Experiment> Experiments { get; set; } = [];
}
