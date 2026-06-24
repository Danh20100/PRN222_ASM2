using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiModels",
                columns: table => new
                {
                    AiModelId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApiEndpoint = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    MaxTokens = table.Column<int>(type: "int", nullable: false),
                    Temperature = table.Column<double>(type: "float", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiModels", x => x.AiModelId);
                });

            migrationBuilder.CreateTable(
                name: "ChunkingStrategies",
                columns: table => new
                {
                    ChunkingStrategyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StrategyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StrategyType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChunkSize = table.Column<int>(type: "int", nullable: false),
                    ChunkOverlap = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChunkingStrategies", x => x.ChunkingStrategyId);
                });

            migrationBuilder.CreateTable(
                name: "EmbeddingModels",
                columns: table => new
                {
                    EmbeddingModelId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApiEndpoint = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    VectorDimension = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmbeddingModels", x => x.EmbeddingModelId);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    SubjectId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.SubjectId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Student"),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    ChapterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    ChapterName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => x.ChapterId);
                    table.ForeignKey(
                        name: "FK_Chapters_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Experiments",
                columns: table => new
                {
                    ExperimentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    EmbeddingModelId = table.Column<int>(type: "int", nullable: false),
                    AiModelId = table.Column<int>(type: "int", nullable: false),
                    ChunkingStrategyId = table.Column<int>(type: "int", nullable: false),
                    ExperimentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TopK = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Experiments", x => x.ExperimentId);
                    table.ForeignKey(
                        name: "FK_Experiments_AiModels_AiModelId",
                        column: x => x.AiModelId,
                        principalTable: "AiModels",
                        principalColumn: "AiModelId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Experiments_ChunkingStrategies_ChunkingStrategyId",
                        column: x => x.ChunkingStrategyId,
                        principalTable: "ChunkingStrategies",
                        principalColumn: "ChunkingStrategyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Experiments_EmbeddingModels_EmbeddingModelId",
                        column: x => x.EmbeddingModelId,
                        principalTable: "EmbeddingModels",
                        principalColumn: "EmbeddingModelId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Experiments_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChatSessions",
                columns: table => new
                {
                    ChatSessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    AiModelId = table.Column<int>(type: "int", nullable: false),
                    EmbeddingModelId = table.Column<int>(type: "int", nullable: false),
                    SessionTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TopK = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSessions", x => x.ChatSessionId);
                    table.ForeignKey(
                        name: "FK_ChatSessions_AiModels_AiModelId",
                        column: x => x.AiModelId,
                        principalTable: "AiModels",
                        principalColumn: "AiModelId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatSessions_EmbeddingModels_EmbeddingModelId",
                        column: x => x.EmbeddingModelId,
                        principalTable: "EmbeddingModels",
                        principalColumn: "EmbeddingModelId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatSessions_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubjectTeachers",
                columns: table => new
                {
                    SubjectTeacherId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectTeachers", x => x.SubjectTeacherId);
                    table.ForeignKey(
                        name: "FK_SubjectTeachers_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubjectTeachers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TotalChunks = table.Column<int>(type: "int", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IndexedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_Documents_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "ChapterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TestSets",
                columns: table => new
                {
                    TestSetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExperimentId = table.Column<int>(type: "int", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedAnswer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestSets", x => x.TestSetId);
                    table.ForeignKey(
                        name: "FK_TestSets_Experiments_ExperimentId",
                        column: x => x.ExperimentId,
                        principalTable: "Experiments",
                        principalColumn: "ExperimentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatHistories",
                columns: table => new
                {
                    ChatHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChatSessionId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TokenCount = table.Column<int>(type: "int", nullable: true),
                    LatencyMs = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatHistories", x => x.ChatHistoryId);
                    table.ForeignKey(
                        name: "FK_ChatHistories_ChatSessions_ChatSessionId",
                        column: x => x.ChatSessionId,
                        principalTable: "ChatSessions",
                        principalColumn: "ChatSessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentChunks",
                columns: table => new
                {
                    DocumentChunkId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    EmbeddingModelId = table.Column<int>(type: "int", nullable: false),
                    ChunkIndex = table.Column<int>(type: "int", nullable: false),
                    ChunkText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TokenCount = table.Column<int>(type: "int", nullable: false),
                    EmbeddingJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VectorDimension = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentChunks", x => x.DocumentChunkId);
                    table.ForeignKey(
                        name: "FK_DocumentChunks_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentChunks_EmbeddingModels_EmbeddingModelId",
                        column: x => x.EmbeddingModelId,
                        principalTable: "EmbeddingModels",
                        principalColumn: "EmbeddingModelId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentIndexes",
                columns: table => new
                {
                    DocumentIndexId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    EmbeddingModelId = table.Column<int>(type: "int", nullable: false),
                    ChunkingStrategyId = table.Column<int>(type: "int", nullable: false),
                    TotalChunks = table.Column<int>(type: "int", nullable: false),
                    VectorDimension = table.Column<int>(type: "int", nullable: false),
                    IndexedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IndexingDurationSeconds = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentIndexes", x => x.DocumentIndexId);
                    table.ForeignKey(
                        name: "FK_DocumentIndexes_ChunkingStrategies_ChunkingStrategyId",
                        column: x => x.ChunkingStrategyId,
                        principalTable: "ChunkingStrategies",
                        principalColumn: "ChunkingStrategyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentIndexes_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentIndexes_EmbeddingModels_EmbeddingModelId",
                        column: x => x.EmbeddingModelId,
                        principalTable: "EmbeddingModels",
                        principalColumn: "EmbeddingModelId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BenchmarkResults",
                columns: table => new
                {
                    BenchmarkResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExperimentId = table.Column<int>(type: "int", nullable: false),
                    TestSetId = table.Column<int>(type: "int", nullable: false),
                    GeneratedAnswer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RetrievedChunkIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FaithfulnessScore = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    RelevanceScore = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    ContextRecallScore = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    AnswerSimilarityScore = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    LatencyMs = table.Column<int>(type: "int", nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkResults", x => x.BenchmarkResultId);
                    table.ForeignKey(
                        name: "FK_BenchmarkResults_Experiments_ExperimentId",
                        column: x => x.ExperimentId,
                        principalTable: "Experiments",
                        principalColumn: "ExperimentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BenchmarkResults_TestSets_TestSetId",
                        column: x => x.TestSetId,
                        principalTable: "TestSets",
                        principalColumn: "TestSetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatCitations",
                columns: table => new
                {
                    ChatCitationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChatHistoryId = table.Column<int>(type: "int", nullable: false),
                    DocumentChunkId = table.Column<int>(type: "int", nullable: false),
                    SimilarityScore = table.Column<double>(type: "float", nullable: false),
                    RetrievalRank = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatCitations", x => x.ChatCitationId);
                    table.ForeignKey(
                        name: "FK_ChatCitations_ChatHistories_ChatHistoryId",
                        column: x => x.ChatHistoryId,
                        principalTable: "ChatHistories",
                        principalColumn: "ChatHistoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatCitations_DocumentChunks_DocumentChunkId",
                        column: x => x.DocumentChunkId,
                        principalTable: "DocumentChunks",
                        principalColumn: "DocumentChunkId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "AiModels",
                columns: new[] { "AiModelId", "ApiEndpoint", "CreatedAt", "Description", "IsActive", "IsDefault", "MaxTokens", "ModelName", "Provider", "Temperature" },
                values: new object[] { 1, "https://generativelanguage.googleapis.com/v1beta", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Google Gemini 2.0 Flash — fast & cost-effective", true, true, 8192, "gemini-2.0-flash", "Gemini", 0.69999999999999996 });

            migrationBuilder.InsertData(
                table: "ChunkingStrategies",
                columns: new[] { "ChunkingStrategyId", "ChunkOverlap", "ChunkSize", "CreatedAt", "Description", "IsActive", "IsDefault", "StrategyName", "StrategyType" },
                values: new object[,]
                {
                    { 1, 64, 512, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Split text into fixed-size token windows with overlap", true, true, "Fixed Size 512", "FixedSize" },
                    { 2, 0, 1024, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Split text at paragraph boundaries (double newlines)", true, false, "Paragraph Split", "Paragraph" },
                    { 3, 32, 256, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Split text at sentence boundaries with sliding window", true, false, "Sentence Split", "Sentence" },
                    { 4, 64, 512, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Recursively split using hierarchy: paragraph → sentence → word", true, false, "Recursive Character", "Recursive" }
                });

            migrationBuilder.InsertData(
                table: "EmbeddingModels",
                columns: new[] { "EmbeddingModelId", "ApiEndpoint", "CreatedAt", "Description", "IsActive", "IsDefault", "ModelName", "Provider", "VectorDimension" },
                values: new object[] { 1, "https://generativelanguage.googleapis.com/v1beta", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Google Gemini text-embedding-004 (768 dims)", true, true, "text-embedding-004", "Gemini", 768 });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "CreatedAt", "Email", "FullName", "IsActive", "PasswordHash", "Role", "UpdatedAt", "Username" },
                values: new object[] { 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@rag-lms.edu.vn", "System Administrator", true, "240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9", "Admin", null, "admin" });

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkResults_ExperimentId",
                table: "BenchmarkResults",
                column: "ExperimentId");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkResults_TestSetId",
                table: "BenchmarkResults",
                column: "TestSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_SubjectId",
                table: "Chapters",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatCitations_ChatHistoryId",
                table: "ChatCitations",
                column: "ChatHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatCitations_DocumentChunkId",
                table: "ChatCitations",
                column: "DocumentChunkId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatHistories_ChatSessionId",
                table: "ChatHistories",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_AiModelId",
                table: "ChatSessions",
                column: "AiModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_EmbeddingModelId",
                table: "ChatSessions",
                column: "EmbeddingModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_SubjectId",
                table: "ChatSessions",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_UserId",
                table: "ChatSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_DocumentId_ChunkIndex",
                table: "DocumentChunks",
                columns: new[] { "DocumentId", "ChunkIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_EmbeddingModelId",
                table: "DocumentChunks",
                column: "EmbeddingModelId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentIndexes_ChunkingStrategyId",
                table: "DocumentIndexes",
                column: "ChunkingStrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentIndexes_DocumentId",
                table: "DocumentIndexes",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentIndexes_EmbeddingModelId",
                table: "DocumentIndexes",
                column: "EmbeddingModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ChapterId",
                table: "Documents",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_UploadedByUserId",
                table: "Documents",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmbeddingModels_ModelName",
                table: "EmbeddingModels",
                column: "ModelName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Experiments_AiModelId",
                table: "Experiments",
                column: "AiModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Experiments_ChunkingStrategyId",
                table: "Experiments",
                column: "ChunkingStrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_Experiments_EmbeddingModelId",
                table: "Experiments",
                column: "EmbeddingModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Experiments_SubjectId",
                table: "Experiments",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectTeachers_SubjectId",
                table: "SubjectTeachers",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectTeachers_UserId_SubjectId",
                table: "SubjectTeachers",
                columns: new[] { "UserId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestSets_ExperimentId",
                table: "TestSets",
                column: "ExperimentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BenchmarkResults");

            migrationBuilder.DropTable(
                name: "ChatCitations");

            migrationBuilder.DropTable(
                name: "DocumentIndexes");

            migrationBuilder.DropTable(
                name: "SubjectTeachers");

            migrationBuilder.DropTable(
                name: "TestSets");

            migrationBuilder.DropTable(
                name: "ChatHistories");

            migrationBuilder.DropTable(
                name: "DocumentChunks");

            migrationBuilder.DropTable(
                name: "Experiments");

            migrationBuilder.DropTable(
                name: "ChatSessions");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "ChunkingStrategies");

            migrationBuilder.DropTable(
                name: "AiModels");

            migrationBuilder.DropTable(
                name: "EmbeddingModels");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Subjects");
        }
    }
}
