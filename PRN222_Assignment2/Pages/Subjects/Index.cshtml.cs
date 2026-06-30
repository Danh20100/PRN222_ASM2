using BusinessLayer.DTOs;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using PRN222_Assignment2.Hubs;

namespace PRN222_Assignment2.Pages.Subjects;

public class IndexModel : PageModel
{
    private readonly ISubjectService _subjectService;
    private readonly IHubContext<SubjectHub> _hubContext;

    public IndexModel(ISubjectService subjectService, IHubContext<SubjectHub> hubContext)
    {
        _subjectService = subjectService;
        _hubContext = hubContext;
    }

    public IEnumerable<SubjectDto> Subjects { get; set; } = [];
    public bool IsAdmin { get; set; }
    public bool IsTeacher { get; set; }
    public bool IsStudent { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    public async Task OnGetAsync()
    {
        IsAdmin = User.IsInRole("Admin");
        IsTeacher = User.IsInRole("Teacher");
        IsStudent = User.IsInRole("Student");

        if (IsAdmin)
        {
            AllTeachers = await _subjectService.GetTeachersAsync();
            Subjects = await _subjectService.GetAllAsync();
        }
        else if (IsTeacher)
        {
            if (Filter == "mine")
            {
                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    Subjects = await _subjectService.GetTeacherSubjectsAsync(userId);
                }
                ViewData["ActivePage"] = "MySubjects";
            }
            else
            {
                Subjects = await _subjectService.GetAllAsync();
                ViewData["ActivePage"] = "Subjects";
            }
        }
        else
        {
            Subjects = await _subjectService.GetAllAsync();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int subjectId)
    {
        if (!User.IsInRole("Admin"))
        {
            TempData["Error"] = "Bạn không có quyền xóa môn học.";
            return RedirectToPage();
        }

        try
        {
            var success = await _subjectService.DeleteAsync(subjectId);
            if (success)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveSubjectUpdate", "delete", subjectId);
                TempData["Success"] = "Đã xóa môn học thành công.";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy môn học để xóa.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi khi xóa môn học: {ex.Message}";
        }

        return RedirectToPage();
    }

    [BindProperty]
    public int EditSubjectId { get; set; }

    [BindProperty]
    public string EditSubjectCode { get; set; } = string.Empty;

    [BindProperty]
    public string EditSubjectName { get; set; } = string.Empty;

    [BindProperty]
    public int? EditTeacherId { get; set; }

    public IEnumerable<UserDto> AllTeachers { get; set; } = [];

    public async Task<IActionResult> OnPostEditAsync()
    {
        if (!User.IsInRole("Admin"))
        {
            TempData["Error"] = "Bạn không có quyền sửa môn học.";
            return RedirectToPage();
        }

        try
        {
            var updateDto = new CreateSubjectDto 
            { 
                SubjectCode = EditSubjectCode,
                SubjectName = EditSubjectName
            };
            var success = await _subjectService.UpdateAsync(EditSubjectId, updateDto);

            if (success && EditTeacherId.HasValue && User.IsInRole("Admin"))
            {
                var teacherIds = new List<int> { EditTeacherId.Value };
                await _subjectService.AssignTeachersAsync(EditSubjectId, teacherIds, teacherIds);
            }

            if (success)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveSubjectUpdate", "update", new { subjectId = EditSubjectId, subjectCode = EditSubjectCode, subjectName = EditSubjectName });
                TempData["Success"] = "Cập nhật môn học thành công.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi: {ex.Message}";
        }

        return RedirectToPage();
    }
}
