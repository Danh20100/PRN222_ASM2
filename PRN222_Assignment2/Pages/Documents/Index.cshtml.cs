using BusinessLayer.DTOs;
using BusinessLayer.Services;
using DataAccessLayer.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace PRN222_Assignment2.Pages.Documents;

public class IndexModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly ISubjectService _subjectService;
    private readonly IUnitOfWork _uow;

    public IndexModel(
        IDocumentService documentService,
        ISubjectService subjectService,
        IUnitOfWork uow)
    {
        _documentService = documentService;
        _subjectService = subjectService;
        _uow = uow;
    }

    public IEnumerable<DocumentDto> Documents { get; set; } = [];
    public IEnumerable<SubjectDto> Subjects { get; set; } = [];
    public IEnumerable<ChapterDto> AllChapters { get; set; } = [];
    public IEnumerable<ChunkingStrategyDto> ChunkingStrategies { get; set; } = [];
    public IEnumerable<EmbeddingModelDto> EmbeddingModels { get; set; } = [];

    public int? SelectedSubjectId { get; set; }
    public string? SelectedStatus { get; set; }
    public List<int> HeadSubjectIds { get; set; } = new();

    public async Task OnGetAsync(int? subjectId, string? status)
    {
        SelectedSubjectId = subjectId;
        SelectedStatus = status;
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        
        if (User.IsInRole("Teacher"))
        {
            Subjects = await _subjectService.GetTeacherSubjectsAsync(userId);
            HeadSubjectIds = await _uow.SubjectTeachers.Query()
                .Where(st => st.UserId == userId && st.IsSubjectHead)
                .Select(st => st.SubjectId)
                .ToListAsync();
        }
        else
        {
            Subjects = await _subjectService.GetAllAsync();
        }
        ChunkingStrategies = await _subjectService.GetChunkingStrategiesAsync();
        EmbeddingModels = await _subjectService.GetEmbeddingModelsAsync();

        // Load all chapters for upload modal (only for subjects the teacher is Head of)
        var uploadableSubjects = User.IsInRole("Admin") 
            ? new List<SubjectDto>() // Admin cannot upload
            : Subjects.Where(s => HeadSubjectIds.Contains(s.SubjectId)).ToList();

        AllChapters = (await Task.WhenAll(
            uploadableSubjects.Select(s => _subjectService.GetChaptersAsync(s.SubjectId))))
            .SelectMany(c => c)
            .ToList();

        // Load documents with filters
        IEnumerable<DocumentDto> docs;
        if (subjectId.HasValue)
            docs = await _documentService.GetBySubjectAsync(subjectId.Value);
        else
        {
            var allDocs = new List<DocumentDto>();
            foreach (var s in Subjects)
                allDocs.AddRange(await _documentService.GetBySubjectAsync(s.SubjectId));
            docs = allDocs;
        }

        if (!string.IsNullOrEmpty(status))
            docs = docs.Where(d => d.Status == status);

        Documents = docs.OrderByDescending(d => d.UploadedAt);
    }

    public async Task<IActionResult> OnPostUploadAsync(
        int chapterId, int chunkingStrategyId, int embeddingModelId,
        IFormFile uploadFile)
    {
        if (User.IsInRole("Admin"))
        {
            TempData["Error"] = "Quản trị viên không được phép upload tài liệu.";
            return RedirectToPage();
        }

        if (uploadFile == null || uploadFile.Length == 0)
        {
            TempData["Error"] = "Vui lòng chọn file để upload.";
            return RedirectToPage();
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        // Read file into bytes to pass to service (keeps BusinessLayer free of IFormFile)
        using var ms = new MemoryStream();
        await uploadFile.CopyToAsync(ms);

        var ext = Path.GetExtension(uploadFile.FileName).TrimStart('.');

        var dto = new UploadDocumentDto
        {
            ChapterId = chapterId,
            ChunkingStrategyId = chunkingStrategyId,
            EmbeddingModelId = embeddingModelId,
            FileBytes = ms.ToArray(),
            OriginalFileName = uploadFile.FileName,
            FileType = ext,
            FileSizeBytes = uploadFile.Length
        };

        // Validate SubjectHead
        var chapter = await _uow.Chapters.GetByIdAsync(chapterId);
        if (chapter != null)
        {
            bool isHead = await _subjectService.IsSubjectHeadAsync(userId, chapter.SubjectId);
            if (!isHead)
            {
                TempData["Error"] = "Bạn không phải Trưởng bộ môn của môn học này nên không có quyền upload.";
                return RedirectToPage();
            }
        }

        try
        {
            await _documentService.UploadAndIndexAsync(dto, userId);
            TempData["Success"] = $"Đã upload '{uploadFile.FileName}'. Đang tiến hành index trong nền...";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Upload thất bại: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostReIndexAsync(int documentId)
    {
        try
        {
            // Use default strategy for re-index
            var defaultStrategy = (await _subjectService.GetChunkingStrategiesAsync())
                .FirstOrDefault(s => s.IsDefault)?.ChunkingStrategyId ?? 1;
            var defaultModel = (await _subjectService.GetEmbeddingModelsAsync())
                .FirstOrDefault(m => m.IsDefault)?.EmbeddingModelId ?? 1;

            await _documentService.ReIndexAsync(documentId, defaultModel, defaultStrategy);
            TempData["Success"] = "Đã bắt đầu re-index tài liệu.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Re-index thất bại: {ex.Message}";
        }

        return RedirectToPage();
    }
}
