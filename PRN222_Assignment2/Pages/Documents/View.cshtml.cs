using BusinessLayer.DTOs;
using BusinessLayer.Services;
using DataAccessLayer.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace PRN222_Assignment2.Pages.Documents;

[Authorize]
public class ViewModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly IUnitOfWork _uow;

    public ViewModel(IDocumentService documentService, IUnitOfWork uow)
    {
        _documentService = documentService;
        _uow = uow;
    }

    public DocumentDto? Document { get; set; }
    public string? PreviewText { get; set; }
    public List<ChunkPreview> Chunks { get; set; } = [];
    public bool IsFileStream { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, string? mode)
    {
        Document = await _documentService.GetByIdAsync(id);
        if (Document == null)
            return NotFound();

        var entity = await _uow.Documents.GetByIdAsync(id);
        if (entity?.StoragePath == null || !System.IO.File.Exists(entity.StoragePath))
            return NotFound("Không tìm thấy file tài liệu trên máy chủ.");

        var fileType = NormalizeFileType(entity.FileType);

        // Stream raw file (iframe / direct open)
        if (mode == "file" && fileType is "pdf" or "txt" or "md")
        {
            var contentType = fileType switch
            {
                "pdf" => "application/pdf",
                _ => "text/plain; charset=utf-8"
            };
            IsFileStream = true;
            return PhysicalFile(entity.StoragePath, contentType);
        }

        Chunks = await _uow.DocumentChunks.Query()
            .Where(c => c.DocumentId == id)
            .OrderBy(c => c.ChunkIndex)
            .Select(c => new ChunkPreview
            {
                ChunkIndex = c.ChunkIndex,
                TokenCount = c.TokenCount,
                ChunkText = c.ChunkText
            })
            .ToListAsync();

        if (fileType is "pdf" or "txt" or "md")
            return Page();

        PreviewText = await _documentService.GetPreviewTextAsync(id);
        if (string.IsNullOrWhiteSpace(PreviewText))
            PreviewText = "Không thể hiển thị nội dung. Vui lòng tải file về để xem.";

        return Page();
    }

    private static string NormalizeFileType(string? fileType)
        => (fileType ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();

    public class ChunkPreview
    {
        public int ChunkIndex { get; set; }
        public int TokenCount { get; set; }
        public string ChunkText { get; set; } = string.Empty;
    }
}
