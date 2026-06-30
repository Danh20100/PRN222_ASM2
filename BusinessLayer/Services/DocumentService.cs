using BusinessLayer.DTOs;
using BusinessLayer.Helpers;
using BusinessLayer.Interfaces;
using BusinessLayer.Strategies;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using UglyToad.PdfPig;

namespace BusinessLayer.Services;

public interface IDocumentService
{
    Task<DocumentDto?> GetByIdAsync(int documentId);
    Task<IEnumerable<DocumentDto>> GetByChapterAsync(int chapterId);
    Task<IEnumerable<DocumentDto>> GetBySubjectAsync(int subjectId);
    Task<DocumentDto> UploadAndIndexAsync(UploadDocumentDto dto, int uploadedByUserId, CancellationToken cancellationToken = default);
    Task<bool> ReIndexAsync(int documentId, int embeddingModelId, int chunkingStrategyId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int documentId);
    Task<string?> GetPreviewTextAsync(int documentId);
}

public class DocumentService : IDocumentService
{
    private readonly IUnitOfWork _uow;
    private readonly EmbeddingProviderFactory _embeddingFactory;
    private readonly IEnumerable<IChunkingStrategy> _chunkingStrategies;
    private readonly ILogger<DocumentService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDocumentRealtimeNotifier _documentRealtimeNotifier;

    private const string UploadDirectory = "wwwroot/uploads/documents";
    private const string ExtractedTextDirectory = "wwwroot/uploads/extracted-text";

    public DocumentService(
        IUnitOfWork uow,
        EmbeddingProviderFactory embeddingFactory,
        IEnumerable<IChunkingStrategy> chunkingStrategies,
        ILogger<DocumentService> logger,
        IServiceScopeFactory scopeFactory,
        IDocumentRealtimeNotifier documentRealtimeNotifier)
    {
        _uow = uow;
        _embeddingFactory = embeddingFactory;
        _chunkingStrategies = chunkingStrategies;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _documentRealtimeNotifier = documentRealtimeNotifier;
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
        var normalizedOriginalFileName = NormalizeOriginalFileName(dto.OriginalFileName);
        var isDuplicate = await _uow.Documents.AnyAsync(d =>
            d.ChapterId == dto.ChapterId &&
            d.OriginalFileName != null &&
            d.OriginalFileName.ToLower() == normalizedOriginalFileName.ToLower());

        if (isDuplicate)
            throw new InvalidOperationException($"Tài liệu '{normalizedOriginalFileName}' đã tồn tại trong chương này.");

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
            OriginalFileName = normalizedOriginalFileName,
            FileType = dto.FileType.TrimStart('.'),
            FileSizeBytes = dto.FileSizeBytes,
            StoragePath = filePath,
            Status = "Pending",
            UploadedByUserId = uploadedByUserId
        };
        await _uow.Documents.AddAsync(document);
        await _uow.SaveChangesAsync();
        await NotifyDocumentUpdateSafeAsync("create", document.DocumentId, document.Status, cancellationToken);

        var chapter = await _uow.Chapters.GetByIdAsync(document.ChapterId);
        await NotifyProgressAsync(new DocumentProgressUpdate
        {
            DocumentId = document.DocumentId,
            ChapterId = document.ChapterId,
            SubjectId = chapter?.SubjectId,
            FileName = document.OriginalFileName,
            Step = "upload",
            Percent = 20,
            Message = "Upload file thành công",
            Status = document.Status
        }, cancellationToken);

        var (embeddingModelId, chunkingStrategyId) = await ResolveIndexingConfigAsync(
            dto.EmbeddingModelId, dto.ChunkingStrategyId);

        await IndexDocumentAsync(
            document.DocumentId,
            embeddingModelId,
            chunkingStrategyId,
            CancellationToken.None);

        return await GetByIdAsync(document.DocumentId) ?? throw new Exception("Document not found after creation");
    }

    private async Task<(int EmbeddingModelId, int ChunkingStrategyId)> ResolveIndexingConfigAsync(
        int embeddingModelId, int chunkingStrategyId)
    {
        if (embeddingModelId <= 0)
        {
            embeddingModelId = await _uow.EmbeddingModels.Query()
                .Where(m => m.IsDefault && m.IsActive)
                .Select(m => m.EmbeddingModelId)
                .FirstOrDefaultAsync();
            if (embeddingModelId <= 0)
                embeddingModelId = 1;
        }

        if (chunkingStrategyId <= 0)
        {
            chunkingStrategyId = await _uow.ChunkingStrategies.Query()
                .Where(s => s.IsDefault && s.IsActive)
                .Select(s => s.ChunkingStrategyId)
                .FirstOrDefaultAsync();
            if (chunkingStrategyId <= 0)
                chunkingStrategyId = 1;
        }

        return (embeddingModelId, chunkingStrategyId);
    }

    public async Task<bool> ReIndexAsync(
        int documentId, int embeddingModelId, int chunkingStrategyId,
        CancellationToken cancellationToken = default)
    {
        var existingChunks = await _uow.DocumentChunks.FindAsync(c => c.DocumentId == documentId);
        _uow.DocumentChunks.RemoveRange(existingChunks);

        var existingIndexes = await _uow.DocumentIndexes.FindAsync(i => i.DocumentId == documentId);
        _uow.DocumentIndexes.RemoveRange(existingIndexes);

        await _uow.SaveChangesAsync();

        await IndexDocumentAsync(
            documentId,
            embeddingModelId,
            chunkingStrategyId,
            cancellationToken == default ? CancellationToken.None : cancellationToken);
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
        await NotifyDocumentUpdateSafeAsync("status", documentId, document.Status, cancellationToken);

        var chapter = await scopedUow.Chapters.GetByIdAsync(document.ChapterId);
        var subjectId = chapter?.SubjectId;

        await NotifyProgressAsync(new DocumentProgressUpdate
        {
            DocumentId = documentId,
            ChapterId = document.ChapterId,
            SubjectId = subjectId,
            FileName = document.OriginalFileName,
            Step = "processing",
            Percent = 25,
            Message = "Bắt đầu chunk & embed...",
            Status = document.Status
        }, cancellationToken);

        var startTime = DateTime.UtcNow;
        try
        {
            // Load configs
            var embeddingModel = await scopedUow.EmbeddingModels.GetByIdAsync(embeddingModelId)
                ?? throw new Exception($"EmbeddingModel {embeddingModelId} not found");
            var chunkingConfig = await scopedUow.ChunkingStrategies.GetByIdAsync(chunkingStrategyId)
                ?? throw new Exception($"ChunkingStrategy {chunkingStrategyId} not found");

            // Extract text from file
            await NotifyProgressAsync(new DocumentProgressUpdate
            {
                DocumentId = documentId,
                ChapterId = document.ChapterId,
                SubjectId = subjectId,
                FileName = document.OriginalFileName,
                Step = "extract",
                Percent = 35,
                Message = "Đang trích xuất nội dung...",
                Status = "Processing"
            }, cancellationToken);

            var text = await ExtractTextAsync(document.StoragePath!, document.FileType!);
            if (string.IsNullOrWhiteSpace(text))
                throw new Exception("Could not extract text from document");

            await NotifyProgressAsync(new DocumentProgressUpdate
            {
                DocumentId = documentId,
                ChapterId = document.ChapterId,
                SubjectId = subjectId,
                FileName = document.OriginalFileName,
                Step = "extract",
                Percent = 45,
                Message = "Đã trích xuất nội dung",
                Status = "Processing"
            }, cancellationToken);

            // Persist extracted plain text for traceability and debugging.
            var extractedTextPath = GetExtractedTextPath(document.FileName!);
            var extractedTextFolder = Path.GetDirectoryName(extractedTextPath)!;
            Directory.CreateDirectory(extractedTextFolder);
            await File.WriteAllTextAsync(extractedTextPath, text, Encoding.UTF8, cancellationToken);

            // Apply chunking strategy
            var strategy = scopedChunkingStrategies
                .FirstOrDefault(s => s.StrategyType == chunkingConfig.StrategyType)
                ?? throw new Exception($"No chunking strategy for type '{chunkingConfig.StrategyType}'");

            var chunks = strategy.Chunk(text, chunkingConfig.ChunkSize, chunkingConfig.ChunkOverlap);
            chunks = ChunkTextHelper.EnforceMaxWords(chunks, chunkingConfig.ChunkSize, chunkingConfig.ChunkOverlap);

            if (chunks.Count == 0)
                throw new Exception("Chunking produced zero chunks");

            await NotifyProgressAsync(new DocumentProgressUpdate
            {
                DocumentId = documentId,
                ChapterId = document.ChapterId,
                SubjectId = subjectId,
                FileName = document.OriginalFileName,
                Step = "chunk",
                Percent = 50,
                Message = $"Đã tạo {chunks.Count} chunks",
                Status = "Processing",
                TotalChunks = chunks.Count,
                ProcessedChunks = chunks.Count
            }, cancellationToken);

            _logger.LogInformation("Document {Id}: {Count} chunks created using {Strategy}",
                documentId, chunks.Count, chunkingConfig.StrategyType);

            // Get embedding provider
            var provider = scopedEmbeddingFactory.Create(embeddingModel);

            // Embed chunks one-by-one for progress reporting
            var embeddings = new List<float[]>();
            for (var i = 0; i < chunks.Count; i++)
            {
                embeddings.Add(await provider.EmbedAsync(chunks[i], cancellationToken));
                var embedPercent = 50 + (int)Math.Round(40.0 * (i + 1) / chunks.Count);
                await NotifyProgressAsync(new DocumentProgressUpdate
                {
                    DocumentId = documentId,
                    ChapterId = document.ChapterId,
                    SubjectId = subjectId,
                    FileName = document.OriginalFileName,
                    Step = "embed",
                    Percent = embedPercent,
                    Message = $"Đang embed chunk {i + 1}/{chunks.Count}...",
                    Status = "Processing",
                    TotalChunks = chunks.Count,
                    ProcessedChunks = i + 1
                }, cancellationToken);
            }

            // Save chunks to DB
            var documentChunks = chunks
                .Select((chunkText, i) => new DocumentChunk
                {
                    DocumentId = documentId,
                    EmbeddingModelId = embeddingModelId,
                    ChunkIndex = i,
                    ChunkText = chunkText,
                    TokenCount = ChunkTextHelper.CountWords(chunkText),
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
            document.ErrorMessage = null;
            document.TotalChunks = chunks.Count;
            document.IndexedAt = DateTime.UtcNow;
            scopedUow.Documents.Update(document);

            await scopedUow.SaveChangesAsync();
            await NotifyDocumentUpdateSafeAsync("status", documentId, document.Status, cancellationToken);
            await NotifyProgressAsync(new DocumentProgressUpdate
            {
                DocumentId = documentId,
                ChapterId = document.ChapterId,
                SubjectId = subjectId,
                FileName = document.OriginalFileName,
                Step = "done",
                Percent = 100,
                Message = $"Hoàn tất — {chunks.Count} chunks đã embed",
                Status = document.Status,
                TotalChunks = chunks.Count,
                ProcessedChunks = chunks.Count
            }, cancellationToken);
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
                    await NotifyDocumentUpdateSafeAsync("status", documentId, doc.Status, cancellationToken);
                    await NotifyProgressAsync(new DocumentProgressUpdate
                    {
                        DocumentId = documentId,
                        ChapterId = doc.ChapterId,
                        FileName = doc.OriginalFileName,
                        Step = "failed",
                        Percent = 100,
                        Message = "Chunk & embed thất bại",
                        Status = doc.Status,
                        ErrorMessage = doc.ErrorMessage
                    }, cancellationToken);
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
        var normalizedFileType = fileType.Trim().TrimStart('.').ToLowerInvariant();

        return normalizedFileType switch
        {
            "txt" or "md" => await File.ReadAllTextAsync(filePath),
            "pdf" => ExtractPdfText(filePath),
            "docx" => ExtractDocxText(filePath),
            "doc" => throw new NotSupportedException("File type .doc is not supported yet. Please convert to .docx."),
            _ => throw new NotSupportedException($"Unsupported file type '{fileType}'. Supported types: .pdf, .docx, .txt, .md.")
        };
    }

    private static string ExtractPdfText(string filePath)
    {
        var builder = new StringBuilder();

        using var pdf = PdfDocument.Open(filePath);
        foreach (var page in pdf.GetPages())
        {
            var pageText = page.Text;
            if (string.IsNullOrWhiteSpace(pageText))
            {
                pageText = string.Join(' ', page.GetWords().Select(w => w.Text));
            }

            if (!string.IsNullOrWhiteSpace(pageText))
            {
                builder.AppendLine($"--- Trang {page.Number} ---");
                builder.AppendLine(pageText);
                builder.AppendLine();
            }
        }

        return builder.ToString().Trim();
    }

    private static string ExtractDocxText(string filePath)
    {
        try
        {
            using var zip = ZipFile.OpenRead(filePath);
            var entry = zip.GetEntry("word/document.xml");
            if (entry == null) return string.Empty;

            using var stream = entry.Open();
            var xml = XDocument.Load(stream);
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

            var paragraphs = xml.Descendants(w + "p")
                .Select(p => string.Concat(p.Descendants(w + "t").Select(t => t.Value)).Trim())
                .Where(text => !string.IsNullOrWhiteSpace(text));

            return string.Join(Environment.NewLine + Environment.NewLine, paragraphs);
        }
        catch (InvalidDataException)
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

        // Delete extracted text file from disk
        if (!string.IsNullOrWhiteSpace(document.FileName))
        {
            var extractedTextPath = GetExtractedTextPath(document.FileName);
            if (File.Exists(extractedTextPath))
                File.Delete(extractedTextPath);
        }

        _uow.Documents.Remove(document);
        await _uow.SaveChangesAsync();
        await NotifyDocumentUpdateSafeAsync("delete", documentId, null);
        return true;
    }

    public async Task<string?> GetPreviewTextAsync(int documentId)
    {
        var document = await _uow.Documents.GetByIdAsync(documentId);
        if (document?.StoragePath == null || !File.Exists(document.StoragePath))
            return null;

        if (!string.IsNullOrWhiteSpace(document.FileName))
        {
            var extractedTextPath = GetExtractedTextPath(document.FileName);
            if (File.Exists(extractedTextPath))
                return await File.ReadAllTextAsync(extractedTextPath, Encoding.UTF8);
        }

        var chunks = await _uow.DocumentChunks.Query()
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.ChunkIndex)
            .Select(c => c.ChunkText)
            .ToListAsync();

        if (chunks.Count > 0)
            return string.Join(Environment.NewLine + Environment.NewLine, chunks);

        try
        {
            return await ExtractTextAsync(document.StoragePath, document.FileType ?? string.Empty);
        }
        catch
        {
            return null;
        }
    }

    private static string GetExtractedTextPath(string documentFileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(documentFileName);
        var extractedFileName = $"{baseName}.txt";
        return Path.Combine(Directory.GetCurrentDirectory(), ExtractedTextDirectory, extractedFileName);
    }

    private static string NormalizeOriginalFileName(string originalFileName)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
            return "unnamed-file";

        return Path.GetFileName(originalFileName).Trim();
    }

    private async Task NotifyDocumentUpdateSafeAsync(
        string action,
        int documentId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _documentRealtimeNotifier.NotifyDocumentUpdateAsync(action, documentId, status, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to push document realtime update for document {DocumentId}", documentId);
        }
    }

    private async Task NotifyProgressAsync(
        DocumentProgressUpdate progress,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _documentRealtimeNotifier.NotifyDocumentProgressAsync(progress, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to push document progress for document {DocumentId}", progress.DocumentId);
        }
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
