using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Unit of Work — groups all repositories and provides a single SaveChanges.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IGenericRepository<User> Users { get; }
    IGenericRepository<SubjectTeacher> SubjectTeachers { get; }
    IGenericRepository<Subject> Subjects { get; }
    IGenericRepository<Chapter> Chapters { get; }
    IGenericRepository<Document> Documents { get; }
    IGenericRepository<DocumentIndex> DocumentIndexes { get; }
    IGenericRepository<DocumentChunk> DocumentChunks { get; }
    IGenericRepository<EmbeddingModel> EmbeddingModels { get; }
    IGenericRepository<ChunkingStrategy> ChunkingStrategies { get; }
    IGenericRepository<AiModel> AiModels { get; }
    IGenericRepository<ChatSession> ChatSessions { get; }
    IGenericRepository<ChatHistory> ChatHistories { get; }
    IGenericRepository<ChatCitation> ChatCitations { get; }
    IGenericRepository<Experiment> Experiments { get; }
    IGenericRepository<TestSet> TestSets { get; }
    IGenericRepository<BenchmarkResult> BenchmarkResults { get; }

    Task<int> SaveChangesAsync();
}
