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
    public List<ChunkDisplayDto> Chunks { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Document = await _documentService.GetByIdAsync(id);
        if (Document == null)
            return NotFound();

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

        return Page();
    }

    public class ChunkDisplayDto
    {
        public int ChunkIndex { get; set; }
        public int TokenCount { get; set; }
        public string ChunkText { get; set; } = string.Empty;
    }
}
