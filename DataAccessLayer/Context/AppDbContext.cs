using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── User Group ──────────────────────────────────────────────────
    public DbSet<User> Users => Set<User>();
    public DbSet<SubjectTeacher> SubjectTeachers => Set<SubjectTeacher>();

    // ── Document Group ───────────────────────────────────────────────
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentIndex> DocumentIndexes => Set<DocumentIndex>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    // ── AI Group ─────────────────────────────────────────────────────
    public DbSet<EmbeddingModel> EmbeddingModels => Set<EmbeddingModel>();
    public DbSet<ChunkingStrategy> ChunkingStrategies => Set<ChunkingStrategy>();
    public DbSet<AiModel> AiModels => Set<AiModel>();

    // ── Chat Group ───────────────────────────────────────────────────
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatHistory> ChatHistories => Set<ChatHistory>();
    public DbSet<ChatCitation> ChatCitations => Set<ChatCitation>();

    // ── Benchmark Group ──────────────────────────────────────────────
    public DbSet<Experiment> Experiments => Set<Experiment>();
    public DbSet<TestSet> TestSets => Set<TestSet>();
    public DbSet<BenchmarkResult> BenchmarkResults => Set<BenchmarkResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ────────────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Role).HasDefaultValue("Student");
            e.Property(u => u.IsActive).HasDefaultValue(true);
        });

        // ── SubjectTeacher (composite unique) ───────────────────────
        modelBuilder.Entity<SubjectTeacher>(e =>
        {
            e.HasIndex(st => new { st.UserId, st.SubjectId }).IsUnique();
            e.HasOne(st => st.User)
             .WithMany(u => u.SubjectTeachers)
             .HasForeignKey(st => st.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(st => st.Subject)
             .WithMany(s => s.SubjectTeachers)
             .HasForeignKey(st => st.SubjectId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Chapter ─────────────────────────────────────────────────
        modelBuilder.Entity<Chapter>(e =>
        {
            e.HasOne(c => c.Subject)
             .WithMany(s => s.Chapters)
             .HasForeignKey(c => c.SubjectId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Document ────────────────────────────────────────────────
        modelBuilder.Entity<Document>(e =>
        {
            e.HasOne(d => d.Chapter)
             .WithMany(c => c.Documents)
             .HasForeignKey(d => d.ChapterId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(d => d.UploadedBy)
             .WithMany()
             .HasForeignKey(d => d.UploadedByUserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.Property(d => d.Status).HasDefaultValue("Pending");
        });

        // ── DocumentIndex ───────────────────────────────────────────
        modelBuilder.Entity<DocumentIndex>(e =>
        {
            e.HasOne(di => di.Document)
             .WithMany(d => d.DocumentIndexes)
             .HasForeignKey(di => di.DocumentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(di => di.EmbeddingModel)
             .WithMany(em => em.DocumentIndexes)
             .HasForeignKey(di => di.EmbeddingModelId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(di => di.ChunkingStrategy)
             .WithMany(cs => cs.DocumentIndexes)
             .HasForeignKey(di => di.ChunkingStrategyId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── DocumentChunk ───────────────────────────────────────────
        modelBuilder.Entity<DocumentChunk>(e =>
        {
            // EmbeddingJson can be very large — use nvarchar(max)
            e.Property(dc => dc.EmbeddingJson).HasColumnType("nvarchar(max)");
            e.Property(dc => dc.ChunkText).HasColumnType("nvarchar(max)");

            e.HasOne(dc => dc.Document)
             .WithMany(d => d.DocumentChunks)
             .HasForeignKey(dc => dc.DocumentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(dc => dc.EmbeddingModel)
             .WithMany(em => em.DocumentChunks)
             .HasForeignKey(dc => dc.EmbeddingModelId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(dc => new { dc.DocumentId, dc.ChunkIndex });
        });

        // ── EmbeddingModel ───────────────────────────────────────────
        modelBuilder.Entity<EmbeddingModel>(e =>
        {
            e.HasIndex(em => em.ModelName).IsUnique();
        });

        // ── ChatSession ──────────────────────────────────────────────
        modelBuilder.Entity<ChatSession>(e =>
        {
            e.HasOne(cs => cs.User)
             .WithMany(u => u.ChatSessions)
             .HasForeignKey(cs => cs.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(cs => cs.Subject)
             .WithMany(s => s.ChatSessions)
             .HasForeignKey(cs => cs.SubjectId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(cs => cs.AiModel)
             .WithMany(m => m.ChatSessions)
             .HasForeignKey(cs => cs.AiModelId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(cs => cs.EmbeddingModel)
             .WithMany()
             .HasForeignKey(cs => cs.EmbeddingModelId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ChatHistory ──────────────────────────────────────────────
        modelBuilder.Entity<ChatHistory>(e =>
        {
            e.Property(ch => ch.Content).HasColumnType("nvarchar(max)");
            e.HasOne(ch => ch.ChatSession)
             .WithMany(cs => cs.ChatHistories)
             .HasForeignKey(ch => ch.ChatSessionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ChatCitation ─────────────────────────────────────────────
        modelBuilder.Entity<ChatCitation>(e =>
        {
            e.HasOne(cc => cc.ChatHistory)
             .WithMany(ch => ch.Citations)
             .HasForeignKey(cc => cc.ChatHistoryId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(cc => cc.DocumentChunk)
             .WithMany(dc => dc.Citations)
             .HasForeignKey(cc => cc.DocumentChunkId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Experiment ───────────────────────────────────────────────
        modelBuilder.Entity<Experiment>(e =>
        {
            e.HasOne(ex => ex.Subject)
             .WithMany(s => s.Experiments)
             .HasForeignKey(ex => ex.SubjectId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(ex => ex.EmbeddingModel)
             .WithMany()
             .HasForeignKey(ex => ex.EmbeddingModelId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(ex => ex.AiModel)
             .WithMany(m => m.Experiments)
             .HasForeignKey(ex => ex.AiModelId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(ex => ex.ChunkingStrategy)
             .WithMany(cs => cs.Experiments)
             .HasForeignKey(ex => ex.ChunkingStrategyId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── TestSet ──────────────────────────────────────────────────
        modelBuilder.Entity<TestSet>(e =>
        {
            e.Property(ts => ts.Question).HasColumnType("nvarchar(max)");
            e.Property(ts => ts.ExpectedAnswer).HasColumnType("nvarchar(max)");
            e.HasOne(ts => ts.Experiment)
             .WithMany(ex => ex.TestSets)
             .HasForeignKey(ts => ts.ExperimentId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── BenchmarkResult ──────────────────────────────────────────
        modelBuilder.Entity<BenchmarkResult>(e =>
        {
            e.Property(br => br.GeneratedAnswer).HasColumnType("nvarchar(max)");
            e.HasOne(br => br.Experiment)
             .WithMany(ex => ex.BenchmarkResults)
             .HasForeignKey(br => br.ExperimentId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(br => br.TestSet)
             .WithMany(ts => ts.BenchmarkResults)
             .HasForeignKey(br => br.TestSetId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Seed Data ────────────────────────────────────────────────
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Admin user (password: "Admin@123" SHA-256 hashed)
        modelBuilder.Entity<User>().HasData(new User
        {
            UserId = 1,
            Username = "admin",
            Email = "admin@rag-lms.edu.vn",
            PasswordHash = "240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9", // Admin@123
            Role = "Admin",
            FullName = "System Administrator",
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // Default EmbeddingModel (Gemini)
        modelBuilder.Entity<EmbeddingModel>().HasData(new EmbeddingModel
        {
            EmbeddingModelId = 1,
            ModelName = "gemini-embedding-001",
            Provider = "Gemini",
            ApiEndpoint = "https://generativelanguage.googleapis.com/v1beta",
            VectorDimension = 768,
            Description = "Google Gemini gemini-embedding-001 (768 dims)",
            IsDefault = true,
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // Default AiModel (Gemini Flash)
        modelBuilder.Entity<AiModel>().HasData(new AiModel
        {
            AiModelId = 1,
            ModelName = "gemini-2.0-flash",
            Provider = "Gemini",
            ApiEndpoint = "https://generativelanguage.googleapis.com/v1beta",
            MaxTokens = 8192,
            Temperature = 0.7,
            Description = "Google Gemini 2.0 Flash — fast & cost-effective",
            IsDefault = true,
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // 4 Chunking Strategies
        modelBuilder.Entity<ChunkingStrategy>().HasData(
            new ChunkingStrategy
            {
                ChunkingStrategyId = 1,
                StrategyName = "Fixed Size 512",
                StrategyType = "FixedSize",
                ChunkSize = 512,
                ChunkOverlap = 64,
                Description = "Split text into fixed-size token windows with overlap",
                IsDefault = true,
                IsActive = true,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new ChunkingStrategy
            {
                ChunkingStrategyId = 2,
                StrategyName = "Paragraph Split",
                StrategyType = "Paragraph",
                ChunkSize = 1024,
                ChunkOverlap = 0,
                Description = "Split text at paragraph boundaries (double newlines)",
                IsDefault = false,
                IsActive = true,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new ChunkingStrategy
            {
                ChunkingStrategyId = 3,
                StrategyName = "Sentence Split",
                StrategyType = "Sentence",
                ChunkSize = 256,
                ChunkOverlap = 32,
                Description = "Split text at sentence boundaries with sliding window",
                IsDefault = false,
                IsActive = true,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new ChunkingStrategy
            {
                ChunkingStrategyId = 4,
                StrategyName = "Recursive Character",
                StrategyType = "Recursive",
                ChunkSize = 512,
                ChunkOverlap = 64,
                Description = "Recursively split using hierarchy: paragraph → sentence → word",
                IsDefault = false,
                IsActive = true,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
