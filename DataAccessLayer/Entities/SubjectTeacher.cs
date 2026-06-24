using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

public class SubjectTeacher
{
    [Key]
    public int SubjectTeacherId { get; set; }

    [ForeignKey(nameof(User))]
    public int UserId { get; set; }

    [ForeignKey(nameof(Subject))]
    public int SubjectId { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public bool IsSubjectHead { get; set; } = false;

    // Navigation
    public User User { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
}
