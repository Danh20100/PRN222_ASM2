using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

public class ChatSession
{
    [Key]
    public int ChatSessionId { get; set; }

    [ForeignKey(nameof(User))]
    public int UserId { get; set; }

    [ForeignKey(nameof(Subject))]
    public int SubjectId { get; set; }

    [ForeignKey(nameof(AiModel))]
    public int AiModelId { get; set; }

    [ForeignKey(nameof(EmbeddingModel))]
    public int EmbeddingModelId { get; set; }

    [Required, MaxLength(200)]
    public string SessionTitle { get; set; } = "New Chat";

    public int TopK { get; set; } = 3; // Number of chunks to retrieve

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastActivityAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public User User { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public AiModel AiModel { get; set; } = null!;
    public EmbeddingModel EmbeddingModel { get; set; } = null!;
    public ICollection<ChatHistory> ChatHistories { get; set; } = [];
}
