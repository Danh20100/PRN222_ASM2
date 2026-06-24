using BusinessLayer.DTOs;
using BusinessLayer.Helpers;
using BusinessLayer.Strategies;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLayer.Services;

public interface IDocumentService
{
    Task<DocumentDto?> GetByIdAsync(int documentId);
    Task<IEnumerable<DocumentDto>> GetByChapterAsync(int chapterId);
    Task<IEnumerable<DocumentDto>> GetBySubjectAsync(int subjectId);
    Task<DocumentDto> UploadAndIndexAsync(UploadDocumentDto dto, int uploadedByUserId, CancellationToken cancellationToken = default);
    Task<bool> ReIndexAsync(int documentId, int embeddingModelId, int chunkingStrategyId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int documentId);
}

public class DocumentService : IDocumentService
{
    private readonly IUnitOfWork _uow;
    private readonly EmbeddingProviderFactory _embeddingFactory;
    private readonly IEnumerable<IChunkingStrategy> _chunkingStrategies;
    private readonly ILogger<DocumentService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    private const string UploadDirectory = "wwwroot/uploads/documents";

    public DocumentService(
        IUnitOfWork uow,
        EmbeddingProviderFactory embeddingFactory,
        IEnumerable<IChunkingStrategy> chunkingStrategies,
        ILogger<DocumentService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _uow = uow;
        _embeddingFactory = embeddingFactory;
        _chunkingStrategies = chunkingStrategies;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task<DocumentDto?> GetByIdAsync(int documentId)
    {
        var doc = await _uow.Documents.Query()
            .Where(d => d.DocumentId == documentId)
            .Select(d => new DocumentDto
            {
                DocumentId = d.DocumentId,
                ChapterId = d.ChapterId,
                ChapterName = d.Chapter.ChapterName,
                SubjectName = d.Chapter.Subject.SubjectName,
                FileName = d.FileName,
                OriginalFileName = d.OriginalFileName,
                FileType = d.FileType,
                FileSizeBytes = d.FileSizeBytes,
                Status = d.Status,
                ErrorMessage = d.ErrorMessage,
                TotalChunks = d.TotalChunks,
                UploadedAt = d.UploadedAt,
                IndexedAt = d.IndexedAt,
                UploadedByFullName = d.UploadedBy.FullName ?? d.UploadedBy.Username
            })
            .FirstOrDefaultAsync();

        return doc;
    }

    public async Task<IEnumerable<DocumentDto>> GetByChapterAsync(int chapterId)
    {
        return await _uow.Documents.Query()
            .Where(d => d.ChapterId == chapterId)
            .Select(d => new DocumentDto
            {
                DocumentId = d.DocumentId,
                ChapterId = d.ChapterId,
                ChapterName = d.Chapter.ChapterName,
                SubjectName = d.Chapter.Subject.SubjectName,
                FileName = d.FileName,
                OriginalFileName = d.OriginalFileName,
                FileType = d.FileType,
                FileSizeBytes = d.FileSizeBytes,
                Status = d.Status,
                TotalChunks = d.TotalChunks,
                UploadedAt = d.UploadedAt,
                IndexedAt = d.IndexedAt,
                UploadedByFullName = d.UploadedBy.FullName ?? d.UploadedBy.Username
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentDto>> GetBySubjectAsync(int subjectId)
    {
        return await _uow.Documents.Query()
            .Where(d => d.Chapter.SubjectId == subjectId)
            .Select(d => new DocumentDto
            {
                DocumentId = d.DocumentId,
                ChapterId = d.ChapterId,
                ChapterName = d.Chapter.ChapterName,
                SubjectName = d.Chapter.Subject.SubjectName,
                FileName = d.FileName,
                OriginalFileName = d.OriginalFileName,
                FileType = d.FileType,
                FileSizeBytes = d.FileSizeBytes,
                Status = d.Status,
                TotalChunks = d.TotalChunks,
                UploadedAt = d.UploadedAt,
                IndexedAt = d.IndexedAt,
                UploadedByFullName = d.UploadedBy.FullName ?? d.UploadedBy.Username
            })
            .ToListAsync();
    }

    public async Task<DocumentDto> UploadAndIndexAsync(
        UploadDocumentDto dto,
        int uploadedByUserId,
        CancellationToken cancellationToken = default)
    {
        // 1. Save file to disk
        var fileExtension = ("." + dto.FileType).ToLowerInvariant();
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), UploadDirectory);
        Directory.CreateDirectory(uploadPath);
        var filePath = Path.Combine(uploadPath, uniqueFileName);

        await File.WriteAllBytesAsync(filePath, dto.FileBytes, cancellationToken);

        // 2. Create Document record (status = Pending)
        var document = new Document
        {
            ChapterId = dto.ChapterId,
            FileName = uniqueFileName,
            OriginalFileName = dto.OriginalFileName,
            FileType = dto.FileType.TrimStart('.'),
            FileSizeBytes = dto.FileSizeBytes,
            StoragePath = filePath,
            Status = "Pending",
            UploadedByUserId = uploadedByUserId
        };
        await _uow.Documents.AddAsync(document);
        await _uow.SaveChangesAsync();

        // 3. Index asynchronously (background, fire-and-forget)
        _ = Task.Run(() => IndexDocumentAsync(document.DocumentId, dto.EmbeddingModelId,
                                              dto.ChunkingStrategyId, cancellationToken));

        return await GetByIdAsync(document.DocumentId) ?? throw new Exception("Document not found after creation");
    }

    public async Task<bool> ReIndexAsync(
        int documentId, int embeddingModelId, int chunkingStrategyId,
        CancellationToken cancellationToken = default)
    {
        // Delete existing chunks for this document
        var existingChunks = await _uow.DocumentChunks.FindAsync(c => c.DocumentId == documentId);
        _uow.DocumentChunks.RemoveRange(existingChunks);
        await _uow.SaveChangesAsync();

        await IndexDocumentAsync(documentId, embeddingModelId, chunkingStrategyId, cancellationToken);
        return true;
    }

    private async Task IndexDocumentAsync(
        int documentId, int embeddingModelId, int chunkingStrategyId,
        CancellationToken cancellationToken)
    {
        // Create a new scope for background processing to avoid DbContext concurrency issues
        using var scope = _scopeFactory.CreateScope();
        var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var scopedChunkingStrategies = scope.ServiceProvider.GetRequiredService<IEnumerable<IChunkingStrategy>>();
        var scopedEmbeddingFactory = scope.ServiceProvider.GetRequiredService<EmbeddingProviderFactory>();

        // Update status to Processing
        var document = await scopedUow.Documents.GetByIdAsync(documentId);
        if (document is null) return;

        document.Status = "Processing";
        scopedUow.Documents.Update(document);
        await scopedUow.SaveChangesAsync();

        var startTime = DateTime.UtcNow;
        try
        {
            // Load configs
            var embeddingModel = await scopedUow.EmbeddingModels.GetByIdAsync(embeddingModelId)
                ?? throw new Exception($"EmbeddingModel {embeddingModelId} not found");
            var chunkingConfig = await scopedUow.ChunkingStrategies.GetByIdAsync(chunkingStrategyId)
                ?? throw new Exception($"ChunkingStrategy {chunkingStrategyId} not found");

            // Extract text from file
            var text = await ExtractTextAsync(document.StoragePath!, document.FileType!);
            if (string.IsNullOrWhiteSpace(text))
                throw new Exception("Could not extract text from document");

            // Apply chunking strategy
            var strategy = scopedChunkingStrategies
                .FirstOrDefault(s => s.StrategyType == chunkingConfig.StrategyType)
                ?? throw new Exception($"No chunking strategy for type '{chunkingConfig.StrategyType}'");

            var chunks = strategy.Chunk(text, chunkingConfig.ChunkSize, chunkingConfig.ChunkOverlap);

            if (chunks.Count == 0)
                throw new Exception("Chunking produced zero chunks");

            _logger.LogInformation("Document {Id}: {Count} chunks created using {Strategy}",
                documentId, chunks.Count, chunkingConfig.StrategyType);

            // Get embedding provider
            var provider = scopedEmbeddingFactory.Create(embeddingModel);

            // Embed all chunks
            var embeddings = await provider.EmbedBatchAsync(chunks, cancellationToken);

            // Save chunks to DB
            var documentChunks = chunks
                .Select((chunkText, i) => new DocumentChunk
                {
                    DocumentId = documentId,
                    EmbeddingModelId = embeddingModelId,
                    ChunkIndex = i,
                    ChunkText = chunkText,
                    TokenCount = chunkText.Split(' ').Length,
                    EmbeddingJson = VectorHelper.SerializeEmbedding(embeddings[i]),
                    VectorDimension = embeddings[i].Length
                })
                .ToList();

            await scopedUow.DocumentChunks.AddRangeAsync(documentChunks);

            // Save DocumentIndex metadata
            var index = new DocumentIndex
            {
                DocumentId = documentId,
                EmbeddingModelId = embeddingModelId,
                ChunkingStrategyId = chunkingStrategyId,
                TotalChunks = chunks.Count,
                VectorDimension = embeddings.FirstOrDefault()?.Length ?? 0,
                IndexedAt = DateTime.UtcNow,
                IndexingDurationSeconds = (DateTime.UtcNow - startTime).TotalSeconds
            };
            await scopedUow.DocumentIndexes.AddAsync(index);

            // Update document status
            document.Status = "Indexed";
            document.TotalChunks = chunks.Count;
            document.IndexedAt = DateTime.UtcNow;
            scopedUow.Documents.Update(document);

            await scopedUow.SaveChangesAsync();
            _logger.LogInformation("Document {Id} indexed successfully with {Count} chunks", documentId, chunks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index document {DocumentId}", documentId);
            try
            {
                var doc = await scopedUow.Documents.GetByIdAsync(documentId);
                if (doc != null)
                {
                    doc.Status = "Failed";
                    doc.ErrorMessage = ex.Message.Length > 1900 ? ex.Message[..1900] : ex.Message;
                    scopedUow.Documents.Update(doc);
                    await scopedUow.SaveChangesAsync();
                }
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to update document status to Failed");
            }
        }
    }

    private static async Task<string> ExtractTextAsync(string filePath, string fileType)
    {
        return fileType.ToLower() switch
        {
            "txt" or "md" => await File.ReadAllTextAsync(filePath),
            "pdf" => ExtractPdfText(filePath),
            "docx" => ExtractDocxText(filePath),
            _ => await File.ReadAllTextAsync(filePath) // fallback
        };
    }

    private static string ExtractPdfText(string filePath)
    {
        // Basic PDF text extraction — reads raw text between stream markers
        // For production: use PdfPig or iTextSharp NuGet package
        try
        {
            var content = File.ReadAllText(filePath, System.Text.Encoding.Latin1);
            var sb = new System.Text.StringBuilder();
            int pos = 0;
            while ((pos = content.IndexOf("BT", pos, StringComparison.Ordinal)) != -1)
            {
                int end = content.IndexOf("ET", pos, StringComparison.Ordinal);
                if (end == -1) break;
                var block = content[(pos + 2)..end];
                // Extract text from Tj and TJ operators
                foreach (System.Text.RegularExpressions.Match m in
                    System.Text.RegularExpressions.Regex.Matches(block, @"\(([^)]*)\)\s*Tj"))
                    sb.Append(m.Groups[1].Value).Append(' ');
                pos = end + 2;
            }
            return sb.ToString().Trim();
        }
        catch
        {
            return File.ReadAllText(filePath, System.Text.Encoding.UTF8);
        }
    }

    private static string ExtractDocxText(string filePath)
    {
        // Basic DOCX extraction — reads word/document.xml from zip
        // For production: use DocumentFormat.OpenXml NuGet package
        try
        {
            using var zip = System.IO.Compression.ZipFile.OpenRead(filePath);
            var entry = zip.GetEntry("word/document.xml");
            if (entry == null) return string.Empty;

            using var stream = entry.Open();
            using var reader = new System.IO.StreamReader(stream);
            var xml = reader.ReadToEnd();

            // Strip XML tags to get plain text
            return System.Text.RegularExpressions.Regex.Replace(xml, "<[^>]+>", " ")
                   .Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">")
                   .Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

    public async Task<bool> DeleteAsync(int documentId)
    {
        var document = await _uow.Documents.GetByIdAsync(documentId);
        if (document is null) return false;

        // Delete file from disk
        if (document.StoragePath != null && File.Exists(document.StoragePath))
            File.Delete(document.StoragePath);

        _uow.Documents.Remove(document);
        await _uow.SaveChangesAsync();
        return true;
    }
}

// EF Core LINQ extension — must be in scope for .Query() usage
file static class QueryableExtensions
{
    public static async Task<List<T>> ToListAsync<T>(this IQueryable<T> query)
        => await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(query);

    public static async Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> query)
        => await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(query);
}
