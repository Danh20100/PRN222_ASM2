using BusinessLayer.DTOs;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using PRN222_Assignment2.Hubs;

namespace PRN222_Assignment2.Pages.Subjects;

public class EditModel : PageModel
{
    private readonly ISubjectService _subjectService;
    private readonly IHubContext<SubjectHub> _hubContext;

    public EditModel(ISubjectService subjectService, IHubContext<SubjectHub> hubContext)
    {
        _subjectService = subjectService;
        _hubContext = hubContext;
    }

    [BindProperty]
    public CreateSubjectDto SubjectDto { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var subject = await _subjectService.GetByIdAsync(id);
        if (subject == null) return NotFound();

        SubjectDto = new CreateSubjectDto
        {
            SubjectCode = subject.SubjectCode,
            SubjectName = subject.SubjectName,
            Description = subject.Description
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var success = await _subjectService.UpdateAsync(id, SubjectDto);
            if (success)
            {
                var updatedSubject = await _subjectService.GetByIdAsync(id);
                await _hubContext.Clients.All.SendAsync("ReceiveSubjectUpdate", "update", updatedSubject);
                
                TempData["Success"] = "Cập nhật môn học thành công.";
                return RedirectToPage("/Subjects/Index");
            }
            else
            {
                TempData["Error"] = "Không tìm thấy môn học để cập nhật.";
                return Page();
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi khi cập nhật môn học: {ex.Message}";
            return Page();
        }
    }
}
