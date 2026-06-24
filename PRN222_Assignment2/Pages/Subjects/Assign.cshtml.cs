using BusinessLayer.DTOs;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PRN222_Assignment2.Pages.Subjects;

[Authorize(Roles = "Admin")]
public class AssignModel : PageModel
{
    private readonly ISubjectService _subjectService;

    public AssignModel(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    public SubjectDto? Subject { get; set; }
    public IEnumerable<UserDto> AllTeachers { get; set; } = new List<UserDto>();
    public IEnumerable<int> AssignedTeacherIds { get; set; } = new List<int>();
    public IEnumerable<int> HeadTeacherIds { get; set; } = new List<int>();

    [BindProperty]
    public List<int> SelectedTeacherIds { get; set; } = new();

    [BindProperty]
    public List<int> SelectedHeadTeacherIds { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int subjectId)
    {
        Subject = await _subjectService.GetByIdAsync(subjectId);
        if (Subject == null) return NotFound();

        AllTeachers = await _subjectService.GetTeachersAsync();
        AssignedTeacherIds = await _subjectService.GetAssignedTeacherIdsAsync(subjectId);
        HeadTeacherIds = await _subjectService.GetHeadTeacherIdsAsync(subjectId);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int subjectId)
    {
        var success = await _subjectService.AssignTeachersAsync(subjectId, SelectedTeacherIds, SelectedHeadTeacherIds);
        if (success)
        {
            TempData["Success"] = "Đã cập nhật phân công giảng viên thành công.";
        }
        else
        {
            TempData["Error"] = "Có lỗi xảy ra khi cập nhật phân công.";
        }

        return RedirectToPage(new { subjectId });
    }
}
