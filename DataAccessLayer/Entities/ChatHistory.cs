using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

/// <summary>
/// A single turn in a chat conversation. Role: user | assistant | system
/// </summary>
public class ChatHistory
{
    [Key]
    public int ChatHistoryId { get; set; }

    [ForeignKey(nameof(ChatSession))]
    public int ChatSessionId { get; set; }

    [Required, MaxLength(20)]
    public string Role { get; set; } = "user"; // user | assistant | system

    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>Token count for this message (tracked for cost estimation)</summary>
    public int? TokenCount { get; set; }

    /// <summary>LLM response latency in ms (only for assistant messages)</summary>
    public int? LatencyMs { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ChatSession ChatSession { get; set; } = null!;
    public ICollection<ChatCitation> Citations { get; set; } = [];
}
