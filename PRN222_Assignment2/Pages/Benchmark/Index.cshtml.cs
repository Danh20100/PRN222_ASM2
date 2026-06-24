using BusinessLayer.DTOs;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace PRN222_Assignment2.Pages.Benchmark;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IBenchmarkService _benchmarkService;
    private readonly ISubjectService _subjectService;

    public IndexModel(IBenchmarkService benchmarkService, ISubjectService subjectService)
    {
        _benchmarkService = benchmarkService;
        _subjectService = subjectService;
    }

    public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();
    public IEnumerable<ExperimentDto> Experiments { get; set; } = new List<ExperimentDto>();

    [BindProperty(SupportsGet = true)]
    public int? SelectedSubjectId { get; set; }

    public async Task OnGetAsync()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        
        if (User.IsInRole("Teacher"))
            Subjects = await _subjectService.GetTeacherSubjectsAsync(userId);
        else
            Subjects = await _subjectService.GetAllAsync();

        if (SelectedSubjectId.HasValue)
        {
            // Verify permission
            if (User.IsInRole("Teacher") && !Subjects.Any(s => s.SubjectId == SelectedSubjectId.Value))
            {
                SelectedSubjectId = null; // Deny access
            }
            else
            {
                Experiments = await _benchmarkService.GetExperimentsAsync(SelectedSubjectId.Value);
            }
        }
    }

    public async Task<IActionResult> OnPostRunExperimentAsync(int experimentId, int subjectId)
    {
        // Fire and forget the background execution to avoid blocking the request
        // In a real prod app, use IHostedService or Hangfire, but for this assignment Task.Run is okay.
        _ = Task.Run(() =>
        {
            // We need a separate scope since HttpContext is disposed
            // However, BenchmarkService already uses IUnitOfWork injected scoped. 
            // Better to just let it run async directly and wait. Since this is an assignment, we'll await it to avoid DbContext scope issues.
            // Awaiting it means the browser will spin, which is fine for small test sets.
        });

        try
        {
            await _benchmarkService.RunExperimentAsync(experimentId);
            TempData["Success"] = "Đã chạy Benchmark thành công!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Lỗi khi chạy Benchmark: " + ex.Message;
        }

        return RedirectToPage(new { subjectId = subjectId });
    }
}
