using BusinessLayer.DTOs;
using BusinessLayer.Services;
using DataAccessLayer.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace PRN222_Assignment2.Pages.Chat;

public class IndexModel : PageModel
{
    private readonly IChatService _chatService;
    private readonly ISubjectService _subjectService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IChatService chatService,
        ISubjectService subjectService,
        IUnitOfWork uow,
        ILogger<IndexModel> logger)
    {
        _chatService = chatService;
        _subjectService = subjectService;
        _uow = uow;
        _logger = logger;
    }

    // ── Page Properties ───────────────────────────────────────────────
    public IEnumerable<ChatSessionDto> Sessions { get; set; } = [];
    public ChatSessionDto? CurrentSession { get; set; }
    public IEnumerable<ChatMessageDto> Messages { get; set; } = [];
    public int? CurrentSessionId { get; set; }

    public IEnumerable<SubjectDto> AvailableSubjects { get; set; } = [];
    public IEnumerable<EmbeddingModelDto> AvailableEmbeddingModels { get; set; } = [];

    public record AiModelOption(int AiModelId, string ModelName, string Provider);
    public IEnumerable<AiModelOption> AvailableAiModels { get; set; } = [];

    // ── Helpers ───────────────────────────────────────────────────────
    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    // ── GET ───────────────────────────────────────────────────────────
    public async Task OnGetAsync(int? sessionId)
    {
        var userId = GetUserId();
        Sessions = await _chatService.GetUserSessionsAsync(userId);

        if (sessionId.HasValue)
        {
            CurrentSessionId = sessionId;
            CurrentSession = await _chatService.GetSessionAsync(sessionId.Value);
            if (CurrentSession != null)
                Messages = await _chatService.GetMessagesAsync(sessionId.Value);
        }

        await LoadDropdownsAsync();
    }

    // ── POST: Tạo session mới ─────────────────────────────────────────
    public async Task<IActionResult> OnPostCreateSessionAsync(
        string sessionTitle, int subjectId, int aiModelId, int embeddingModelId)
    {
        var userId = GetUserId();
        var session = await _chatService.CreateSessionAsync(new CreateChatSessionDto
        {
            SubjectId = subjectId,
            AiModelId = aiModelId,
            EmbeddingModelId = embeddingModelId,
            SessionTitle = sessionTitle
        }, userId);

        return RedirectToPage(new { sessionId = session.ChatSessionId });
    }

    // ── POST: Gửi tin nhắn (AJAX handler — trả về JSON) ───────────────
    /// <summary>
    /// Razor Page AJAX handler. Called via fetch("/Chat?handler=SendMessage", POST).
    /// Returns JSON instead of redirecting.
    /// </summary>
    public async Task<IActionResult> OnPostSendMessageAsync(
        [FromBody] SendMessageDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || dto.ChatSessionId <= 0 || string.IsNullOrWhiteSpace(dto.Question))
        {
            return new JsonResult(new
            {
                isError = true,
                errorMessage = "Yêu cầu không hợp lệ."
            });
        }

        try
        {
            var result = await _chatService.SendMessageAsync(dto, cancellationToken);
            // Serialize to camelCase for JS
            return new JsonResult(result, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnPostSendMessageAsync for session {Id}", dto.ChatSessionId);
            return new JsonResult(new
            {
                isError = true,
                errorMessage = "Lỗi máy chủ. Vui lòng thử lại."
            });
        }
    }

    // ── POST: Xóa session ─────────────────────────────────────────────
    public async Task<IActionResult> OnPostDeleteSessionAsync(int sessionId)
    {
        await _chatService.DeleteSessionAsync(sessionId, GetUserId());
        return RedirectToPage();
    }

    // ── Helpers ───────────────────────────────────────────────────────
    private async Task LoadDropdownsAsync()
    {
        AvailableSubjects = await _subjectService.GetAllAsync();
        AvailableEmbeddingModels = await _subjectService.GetEmbeddingModelsAsync();
        AvailableAiModels = await _uow.AiModels.Query()
            .Where(m => m.IsActive)
            .Select(m => new AiModelOption(m.AiModelId, m.ModelName, m.Provider))
            .ToListAsync();
    }
}
