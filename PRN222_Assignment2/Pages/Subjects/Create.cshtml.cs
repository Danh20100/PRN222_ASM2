using BusinessLayer.DTOs;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PRN222_Assignment2.Pages.Subjects;

public class CreateModel : PageModel
{
    private readonly ISubjectService _subjectService;

    public CreateModel(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    [BindProperty]
    public CreateSubjectDto SubjectDto { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await _subjectService.CreateAsync(SubjectDto);
            TempData["Success"] = "Đã tạo môn học thành công.";
            return RedirectToPage("/Subjects/Index");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi khi tạo môn học: {ex.Message}";
            return Page();
        }
    }
}
