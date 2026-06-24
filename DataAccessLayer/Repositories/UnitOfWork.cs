using DataAccessLayer.Context;
using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    // ── Lazy-initialized repositories ───────────────────────────────
    private IGenericRepository<User>? _users;
    private IGenericRepository<SubjectTeacher>? _subjectTeachers;
    private IGenericRepository<Subject>? _subjects;
    private IGenericRepository<Chapter>? _chapters;
    private IGenericRepository<Document>? _documents;
    private IGenericRepository<DocumentIndex>? _documentIndexes;
    private IGenericRepository<DocumentChunk>? _documentChunks;
    private IGenericRepository<EmbeddingModel>? _embeddingModels;
    private IGenericRepository<ChunkingStrategy>? _chunkingStrategies;
    private IGenericRepository<AiModel>? _aiModels;
    private IGenericRepository<ChatSession>? _chatSessions;
    private IGenericRepository<ChatHistory>? _chatHistories;
    private IGenericRepository<ChatCitation>? _chatCitations;
    private IGenericRepository<Experiment>? _experiments;
    private IGenericRepository<TestSet>? _testSets;
    private IGenericRepository<BenchmarkResult>? _benchmarkResults;

    public IGenericRepository<User> Users
        => _users ??= new GenericRepository<User>(_context);
    public IGenericRepository<SubjectTeacher> SubjectTeachers
        => _subjectTeachers ??= new GenericRepository<SubjectTeacher>(_context);
    public IGenericRepository<Subject> Subjects
        => _subjects ??= new GenericRepository<Subject>(_context);
    public IGenericRepository<Chapter> Chapters
        => _chapters ??= new GenericRepository<Chapter>(_context);
    public IGenericRepository<Document> Documents
        => _documents ??= new GenericRepository<Document>(_context);
    public IGenericRepository<DocumentIndex> DocumentIndexes
        => _documentIndexes ??= new GenericRepository<DocumentIndex>(_context);
    public IGenericRepository<DocumentChunk> DocumentChunks
        => _documentChunks ??= new GenericRepository<DocumentChunk>(_context);
    public IGenericRepository<EmbeddingModel> EmbeddingModels
        => _embeddingModels ??= new GenericRepository<EmbeddingModel>(_context);
    public IGenericRepository<ChunkingStrategy> ChunkingStrategies
        => _chunkingStrategies ??= new GenericRepository<ChunkingStrategy>(_context);
    public IGenericRepository<AiModel> AiModels
        => _aiModels ??= new GenericRepository<AiModel>(_context);
    public IGenericRepository<ChatSession> ChatSessions
        => _chatSessions ??= new GenericRepository<ChatSession>(_context);
    public IGenericRepository<ChatHistory> ChatHistories
        => _chatHistories ??= new GenericRepository<ChatHistory>(_context);
    public IGenericRepository<ChatCitation> ChatCitations
        => _chatCitations ??= new GenericRepository<ChatCitation>(_context);
    public IGenericRepository<Experiment> Experiments
        => _experiments ??= new GenericRepository<Experiment>(_context);
    public IGenericRepository<TestSet> TestSets
        => _testSets ??= new GenericRepository<TestSet>(_context);
    public IGenericRepository<BenchmarkResult> BenchmarkResults
        => _benchmarkResults ??= new GenericRepository<BenchmarkResult>(_context);

    public Task<int> SaveChangesAsync()
        => _context.SaveChangesAsync();

    public void Dispose()
        => _context.Dispose();
}
