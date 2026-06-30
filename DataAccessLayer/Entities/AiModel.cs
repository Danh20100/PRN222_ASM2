using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Entities;

/// <summary>
/// LLM used to generate chat answers. Provider: Gemini | OpenAI | Ollama
/// </summary>
public class AiModel
{
    /// <summary>
    /// The unique identifier of the AI model.
    /// </summary>
    [Key]
    public int AiModelId { get; set; }

    /// <summary>
    /// The technical name of the AI model (e.g., gemini-1.5-flash).
    /// </summary>
    [Required, MaxLength(100)]
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// The provider of the AI model (e.g., Gemini, OpenAI, Ollama).
    /// </summary>
    [Required, MaxLength(50)]
    public string Provider { get; set; } = string.Empty; // Gemini | OpenAI | Ollama

    [MaxLength(300)]
    public string? ApiEndpoint { get; set; }

    public int MaxTokens { get; set; } = 8192;

    public double Temperature { get; set; } = 0.7;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsDefault { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<ChatSession> ChatSessions { get; set; } = [];
    public ICollection<Experiment> Experiments { get; set; } = [];
}
