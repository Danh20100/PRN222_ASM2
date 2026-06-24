using BusinessLayer.DTOs;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PRN222_Assignment2.Pages.Subjects;

public class IndexModel : PageModel
{
    private readonly ISubjectService _subjectService;

    public IndexModel(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    public IEnumerable<SubjectDto> Subjects { get; set; } = [];
    public bool IsAdmin { get; set; }
    public bool IsTeacher { get; set; }
    public bool IsStudent { get; set; }

    public async Task OnGetAsync()
    {
        IsAdmin = User.IsInRole("Admin");
        IsTeacher = User.IsInRole("Teacher");
        IsStudent = User.IsInRole("Student");

        if (IsTeacher)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                Subjects = await _subjectService.GetTeacherSubjectsAsync(userId);
            }
        }
        else
        {
            Subjects = await _subjectService.GetAllAsync();
        }
    }
}
