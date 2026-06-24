using BusinessLayer.DTOs;
using BusinessLayer.Services;
using PRN222_Assignment2.Models;
using DataAccessLayer.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace PRN222_Assignment2.Pages.Benchmark;

[Authorize(Roles = "Admin,Teacher")]
public class CreateModel : PageModel
{
    private readonly IBenchmarkService _benchmarkService;
    private readonly ISubjectService _subjectService;
    private readonly IUnitOfWork _uow;

    public CreateModel(IBenchmarkService benchmarkService, ISubjectService subjectService, IUnitOfWork uow)
    {
        _benchmarkService = benchmarkService;
        _subjectService = subjectService;
        _uow = uow;
    }

    [BindProperty]
    public CreateExperimentViewModel Input { get; set; } = new();

    public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();
    public IEnumerable<EmbeddingModelDto> EmbeddingModels { get; set; } = new List<EmbeddingModelDto>();
    public IEnumerable<ChunkingStrategyDto> ChunkingStrategies { get; set; } = new List<ChunkingStrategyDto>();
    
    public record AiModelOption(int AiModelId, string ModelName);
    public IEnumerable<AiModelOption> AiModels { get; set; } = new List<AiModelOption>();

    public async Task OnGetAsync()
    {
        await LoadDropdownsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return Page();
        }

        try
        {
            var dto = new CreateExperimentDto
            {
                SubjectId = Input.SubjectId,
                EmbeddingModelId = Input.EmbeddingModelId,
                AiModelId = Input.AiModelId,
                ChunkingStrategyId = Input.ChunkingStrategyId,
                ExperimentName = Input.ExperimentName,
                Description = Input.Description,
                TopK = Input.TopK
            };
            var experiment = await _benchmarkService.CreateExperimentAsync(dto);
            return RedirectToPage("Details", new { experimentId = experiment.ExperimentId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Lỗi khi tạo Experiment: " + ex.Message;
            await LoadDropdownsAsync();
            return Page();
        }
    }

    private async Task LoadDropdownsAsync()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        
        if (User.IsInRole("Teacher"))
            Subjects = await _subjectService.GetTeacherSubjectsAsync(userId);
        else
            Subjects = await _subjectService.GetAllAsync();

        EmbeddingModels = await _subjectService.GetEmbeddingModelsAsync();
        ChunkingStrategies = await _subjectService.GetChunkingStrategiesAsync();
        
        AiModels = await _uow.AiModels.Query()
            .Where(m => m.IsActive)
            .Select(m => new AiModelOption(m.AiModelId, m.ModelName))
            .ToListAsync();
    }
}
