using BusinessLayer.DTOs;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace PRN222_Assignment2.Pages.Subjects;

public class DetailsModel : PageModel
{
    private readonly ISubjectService _subjectService;

    public DetailsModel(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    public SubjectDto? Subject { get; set; }
    public IEnumerable<ChapterDto> Chapters { get; set; } = [];

    [BindProperty]
    public CreateChapterDto NewChapter { get; set; } = new();

    public bool CanEdit { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Subject = await _subjectService.GetByIdAsync(id);
        if (Subject == null) return NotFound();

        Chapters = await _subjectService.GetChaptersAsync(id);

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        bool isAdmin = User.IsInRole("Admin");
        bool isHead = await _subjectService.IsSubjectHeadAsync(userId, id);

        CanEdit = isAdmin || isHead;

        return Page();
    }

    public async Task<IActionResult> OnPostCreateChapterAsync(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        bool isAdmin = User.IsInRole("Admin");
        bool isHead = await _subjectService.IsSubjectHeadAsync(userId, id);

        if (!isAdmin && !isHead)
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

    public async Task<IActionResult> OnPostDeleteChapterAsync(int id, int chapterId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        bool isAdmin = User.IsInRole("Admin");
        bool isHead = await _subjectService.IsSubjectHeadAsync(userId, id);

        if (!isAdmin && !isHead)
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
}
