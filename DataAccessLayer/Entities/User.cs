using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities;

public class User
{
    [Key]
    public int UserId { get; set; }

    [Required, MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>SHA-256 hashed password</summary>
    [Required, MaxLength(64)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Role { get; set; } = "Student"; // Admin | Teacher | Student

    [MaxLength(200)]
    public string? FullName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<SubjectTeacher> SubjectTeachers { get; set; } = [];
    public ICollection<ChatSession> ChatSessions { get; set; } = [];
}
