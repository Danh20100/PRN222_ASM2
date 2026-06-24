using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities;

/// <summary>
/// Links an assistant response to the DocumentChunks that were retrieved (RAG citations).
/// </summary>
public class ChatCitation
{
    [Key]
    public int ChatCitationId { get; set; }

    [ForeignKey(nameof(ChatHistory))]
    public int ChatHistoryId { get; set; }

    [ForeignKey(nameof(DocumentChunk))]
    public int DocumentChunkId { get; set; }

    /// <summary>Cosine similarity score [0..1] of this chunk to the user question</summary>
    public double SimilarityScore { get; set; }

    /// <summary>Rank of this chunk in the retrieval results (1 = most relevant)</summary>
    public int RetrievalRank { get; set; }

    // Navigation
    public ChatHistory ChatHistory { get; set; } = null!;
    public DocumentChunk DocumentChunk { get; set; } = null!;
}
