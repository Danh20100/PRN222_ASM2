using BusinessLayer.DTOs;
using BusinessLayer.Services;
using PRN222_Assignment2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PRN222_Assignment2.Pages.Benchmark;

[Authorize(Roles = "Admin,Teacher")]
public class DetailsModel : PageModel
{
    private readonly IBenchmarkService _benchmarkService;

    public DetailsModel(IBenchmarkService benchmarkService)
    {
        _benchmarkService = benchmarkService;
    }

    public ExperimentDto? Experiment { get; set; }
    public IEnumerable<BenchmarkResultDto> Results { get; set; } = new List<BenchmarkResultDto>();
    public int SubjectId { get; set; }

    [BindProperty]
    public AddTestCaseViewModel TestCaseInput { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int experimentId)
    {
        Experiment = await _benchmarkService.GetExperimentAsync(experimentId);
        if (Experiment == null) return NotFound();

        SubjectId = Experiment.SubjectId;
        Results = await _benchmarkService.GetResultsAsync(experimentId);

        return Page();
    }

    public async Task<IActionResult> OnPostAddTestCaseAsync(int experimentId)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Vui lòng nhập đầy đủ câu hỏi và câu trả lời.";
            return RedirectToPage(new { experimentId });
        }

        await _benchmarkService.AddTestCaseAsync(experimentId, TestCaseInput.Question, TestCaseInput.ExpectedAnswer);
        TempData["Success"] = "Đã thêm Test Case thành công.";
        return RedirectToPage(new { experimentId });
    }
}
