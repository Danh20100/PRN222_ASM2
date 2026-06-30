using BusinessLayer.DTOs;
using BusinessLayer.Services;
using DataAccessLayer.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace PRN222_Assignment2.Pages.Documents;

public class DetailsModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly IUnitOfWork _uow;

    public DetailsModel(IDocumentService documentService, IUnitOfWork uow)
    {
        _documentService = documentService;
        _uow = uow;
    }

    public DocumentDto? Document { get; set; }
    public string? PreviewText { get; set; }
    public int ChunkSize { get; set; } = 512;
    public List<ChunkDisplayDto> Chunks { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Document = await _documentService.GetByIdAsync(id);
        if (Document == null)
            return NotFound();

        ChunkSize = await _uow.DocumentIndexes.Query()
            .Where(i => i.DocumentId == id)
            .Join(_uow.ChunkingStrategies.Query(),
                i => i.ChunkingStrategyId,
                s => s.ChunkingStrategyId,
                (_, s) => s.ChunkSize)
            .FirstOrDefaultAsync();

        if (ChunkSize <= 0)
        {
            ChunkSize = await _uow.ChunkingStrategies.Query()
                .Where(s => s.IsDefault)
                .Select(s => s.ChunkSize)
                .FirstOrDefaultAsync();
        }

        if (ChunkSize <= 0)
            ChunkSize = 512;

        Chunks = await _uow.DocumentChunks.Query()
            .Where(c => c.DocumentId == id)
            .OrderBy(c => c.ChunkIndex)
            .Select(c => new ChunkDisplayDto
            {
                ChunkIndex = c.ChunkIndex,
                TokenCount = c.TokenCount,
                ChunkText = c.ChunkText
            })
            .ToListAsync();

        PreviewText = await _documentService.GetPreviewTextAsync(id);
        if (string.IsNullOrWhiteSpace(PreviewText))
            PreviewText = "Không thể hiển thị nội dung. Vui lòng tải file về để xem.";

        return Page();
    }

    public class ChunkDisplayDto
    {
        public int ChunkIndex { get; set; }
        public int TokenCount { get; set; }
        public string ChunkText { get; set; } = string.Empty;
    }
}
