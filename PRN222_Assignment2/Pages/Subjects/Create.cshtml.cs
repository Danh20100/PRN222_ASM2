using BusinessLayer.DTOs;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using PRN222_Assignment2.Hubs;

namespace PRN222_Assignment2.Pages.Subjects;

public class CreateModel : PageModel
{
    private readonly ISubjectService _subjectService;
    private readonly IHubContext<SubjectHub> _hubContext;

    public CreateModel(ISubjectService subjectService, IHubContext<SubjectHub> hubContext)
    {
        _subjectService = subjectService;
        _hubContext = hubContext;
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
            var newSubject = await _subjectService.CreateAsync(SubjectDto);
            await _hubContext.Clients.All.SendAsync("ReceiveSubjectUpdate", "create", newSubject);

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
