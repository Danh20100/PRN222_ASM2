using BusinessLayer.DTOs;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace PRN222_Assignment2.Pages.Subjects;

public class DetailsModel : PageModel
{
    private readonly ISubjectService _subjectService;
    private readonly IDocumentService _documentService;

    public DetailsModel(ISubjectService subjectService, IDocumentService documentService)
    {
        _subjectService = subjectService;
        _documentService = documentService;
    }

    public SubjectDto? Subject { get; set; }
    public IEnumerable<ChapterDto> Chapters { get; set; } = [];

    public IEnumerable<EmbeddingModelDto> EmbeddingModels { get; set; } = [];
    public IEnumerable<ChunkingStrategyDto> ChunkingStrategies { get; set; } = [];

    [BindProperty]
    public CreateChapterDto NewChapter { get; set; } = new();

    public bool CanEdit { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Subject = await _subjectService.GetByIdAsync(id);
        if (Subject == null) return NotFound();

        Chapters = await _subjectService.GetChaptersAsync(id);

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        bool isHead = await _subjectService.IsSubjectHeadAsync(userId, id);

        CanEdit = isHead;

        if (CanEdit)
        {
            EmbeddingModels = await _subjectService.GetEmbeddingModelsAsync();
            ChunkingStrategies = await _subjectService.GetChunkingStrategiesAsync();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCreateChapterAsync(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        bool isHead = await _subjectService.IsSubjectHeadAsync(userId, id);

        if (!isHead)
        {
            TempData["Error"] = "Bạn không có quyền thao tác trên môn học này.";
            return RedirectToPage(new { id });
        }

        NewChapter.SubjectId = id;
        try
        {
            await _subjectService.CreateChapterAsync(NewChapter);
            TempData["Success"] = "Đã tạo chương học thành công.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostEditChapterAsync(int id, int chapterId, string chapterName, int orderIndex)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        bool isHead = await _subjectService.IsSubjectHeadAsync(userId, id);

        if (!isHead)
        {
            TempData["Error"] = "Bạn không có quyền thao tác trên môn học này.";
            return RedirectToPage(new { id });
        }

        try
        {
            var dto = new CreateChapterDto
            {
                SubjectId = id,
                ChapterName = chapterName,
                OrderIndex = orderIndex
            };
            var success = await _subjectService.UpdateChapterAsync(chapterId, dto);
            if (success)
            {
                TempData["Success"] = "Đã cập nhật tên chương học thành công.";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy chương học cần cập nhật.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteChapterAsync(int id, int chapterId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        bool isHead = await _subjectService.IsSubjectHeadAsync(userId, id);

        if (!isHead)
        {
            TempData["Error"] = "Bạn không có quyền thao tác trên môn học này.";
            return RedirectToPage(new { id });
        }

        try
        {
            await _subjectService.DeleteChapterAsync(chapterId);
            TempData["Success"] = "Đã xóa chương học.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi: {ex.Message}";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUploadDocumentAsync(
        int id, int chapterId, int chunkingStrategyId, int embeddingModelId,
        IFormFile uploadFile)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        bool isHead = await _subjectService.IsSubjectHeadAsync(userId, id);

        if (!isHead)
        {
            TempData["Error"] = "Bạn không có quyền thao tác trên môn học này.";
            return RedirectToPage(new { id });
        }

        if (uploadFile == null || uploadFile.Length == 0)
        {
            TempData["Error"] = "Vui lòng chọn file để upload.";
            return RedirectToPage(new { id });
        }

        using var ms = new MemoryStream();
        await uploadFile.CopyToAsync(ms);

        var ext = Path.GetExtension(uploadFile.FileName).TrimStart('.');

        var dto = new UploadDocumentDto
        {
            ChapterId = chapterId,
            EmbeddingModelId = embeddingModelId,
            ChunkingStrategyId = chunkingStrategyId,
            FileBytes = ms.ToArray(),
            OriginalFileName = uploadFile.FileName,
            FileType = ext.ToUpper(),
            FileSizeBytes = uploadFile.Length
        };

        try
        {
            var result = await _documentService.UploadAndIndexAsync(dto, userId);
            TempData["Success"] = result.Status switch
            {
                "Indexed" => $"Đã upload và chunk & embed thành công ({result.TotalChunks} chunks).",
                "Failed" => $"Upload xong nhưng chunk & embed thất bại: {result.ErrorMessage}",
                _ => "Đã upload tài liệu thành công."
            };
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi upload: {ex.Message}";
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteDocAsync(int id, int documentId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        bool isHead = await _subjectService.IsSubjectHeadAsync(userId, id);

        if (!isHead)
        {
            TempData["Error"] = "Bạn không có quyền thao tác trên môn học này.";
            return RedirectToPage(new { id });
        }

        try
        {
            await _documentService.DeleteAsync(documentId);
            TempData["Success"] = "Đã xóa tài liệu.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi xóa tài liệu: {ex.Message}";
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostChunkAsync(int id, int documentId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        bool isHead = await _subjectService.IsSubjectHeadAsync(userId, id);

        if (!isHead)
        {
            TempData["Error"] = "Bạn không có quyền thao tác trên môn học này.";
            return RedirectToPage(new { id });
        }

        try
        {
            var defaultStrategy = (await _subjectService.GetChunkingStrategiesAsync())
                .FirstOrDefault(s => s.IsDefault)?.ChunkingStrategyId ?? 1;
            var defaultModel = (await _subjectService.GetEmbeddingModelsAsync())
                .FirstOrDefault(m => m.IsDefault)?.EmbeddingModelId ?? 1;

            await _documentService.ReIndexAsync(documentId, defaultModel, defaultStrategy);
            var doc = await _documentService.GetByIdAsync(documentId);
            TempData["Success"] = doc?.Status switch
            {
                "Indexed" => $"Chunk & embed thành công ({doc.TotalChunks} chunks).",
                "Failed" => $"Chunk & embed thất bại: {doc.ErrorMessage}",
                _ => "Đã chạy chunk & embed."
            };
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi: {ex.Message}";
        }
        return RedirectToPage(new { id });
    }
}
