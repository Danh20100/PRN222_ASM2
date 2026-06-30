namespace BusinessLayer.DTOs;

// ── Auth DTOs ────────────────────────────────────────────────────────

public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}

public class UserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Student";
    public string? FullName { get; set; }
}

public class UpdateUserDto
{
    public string? FullName { get; set; }
    public string Role { get; set; } = string.Empty;
}

// ── Subject / Chapter DTOs ───────────────────────────────────────────

public class SubjectDto
{
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int ChapterCount { get; set; }
    public int DocumentCount { get; set; }
}

public class CreateSubjectDto
{
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ChapterDto
{
    public int ChapterId { get; set; }
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string ChapterName { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public string? Description { get; set; }
    public int DocumentCount { get; set; }
    public List<DocumentDto> Documents { get; set; } = new();
}

public class CreateChapterDto
{
    public int SubjectId { get; set; }
    public string ChapterName { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public string? Description { get; set; }
}

// ── Document DTOs ────────────────────────────────────────────────────

public class DocumentDto
{
    public int DocumentId { get; set; }
    public int ChapterId { get; set; }
    public string ChapterName { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public string? FileType { get; set; }
    public long FileSizeBytes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int? TotalChunks { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? IndexedAt { get; set; }
    public string UploadedByFullName { get; set; } = string.Empty;
}

public class UploadDocumentDto
{
    public int ChapterId { get; set; }
    public int EmbeddingModelId { get; set; }
    public int ChunkingStrategyId { get; set; }
    /// <summary>Raw file bytes — populated by the Razor Page before calling the service</summary>
    public byte[] FileBytes { get; set; } = [];
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}

// ── Chat DTOs ────────────────────────────────────────────────────────

public class ChatSessionDto
{
    public int ChatSessionId { get; set; }
    public string SessionTitle { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string AiModelName { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
}

public class CreateChatSessionDto
{
    public int SubjectId { get; set; }
    public int AiModelId { get; set; }
    public int EmbeddingModelId { get; set; }
    public string SessionTitle { get; set; } = "New Chat";
    public int TopK { get; set; } = 3;
}

public class ChatMessageDto
{
    public int ChatHistoryId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? LatencyMs { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CitationDto> Citations { get; set; } = [];
}

public class CitationDto
{
    public int DocumentChunkId { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string ChapterName { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
    public int RetrievalRank { get; set; }
}

public class SendMessageDto
{
    public int ChatSessionId { get; set; }
    public string Question { get; set; } = string.Empty;
}

public class ChatResponseDto
{
    public string Answer { get; set; } = string.Empty;
    public int LatencyMs { get; set; }
    public List<CitationDto> Citations { get; set; } = [];
    public bool IsError { get; set; }
    public string? ErrorMessage { get; set; }
}

// ── Embedding / Chunking DTOs ────────────────────────────────────────

public class EmbeddingModelDto
{
    public int EmbeddingModelId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public int VectorDimension { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public class ChunkingStrategyDto
{
    public int ChunkingStrategyId { get; set; }
    public string StrategyName { get; set; } = string.Empty;
    public string StrategyType { get; set; } = string.Empty;
    public int ChunkSize { get; set; }
    public int ChunkOverlap { get; set; }
    public bool IsDefault { get; set; }
}

// ── Benchmark DTOs ───────────────────────────────────────────────────

public class ExperimentDto
{
    public int ExperimentId { get; set; }
    public int SubjectId { get; set; }
    public string ExperimentName { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string EmbeddingModelName { get; set; } = string.Empty;
    public string AiModelName { get; set; } = string.Empty;
    public string ChunkingStrategyName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TestSetCount { get; set; }
    public double? AvgFaithfulness { get; set; }
    public double? AvgRelevance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class CreateExperimentDto
{
    public int SubjectId { get; set; }
    public int EmbeddingModelId { get; set; }
    public int AiModelId { get; set; }
    public int ChunkingStrategyId { get; set; }
    public string ExperimentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TopK { get; set; } = 3;
}

public class BenchmarkResultDto
{
    public int BenchmarkResultId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string ExpectedAnswer { get; set; } = string.Empty;
    public string? GeneratedAnswer { get; set; }
    public decimal? FaithfulnessScore { get; set; }
    public decimal? RelevanceScore { get; set; }
    public decimal? ContextRecallScore { get; set; }
    public decimal? AnswerSimilarityScore { get; set; }
    public int? LatencyMs { get; set; }
    public string? ErrorMessage { get; set; }
}
