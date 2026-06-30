using System.Net;
using System.Text;
using System.Text.Json;
using BusinessLayer.DTOs;
using BusinessLayer.Helpers;
using BusinessLayer.Models;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Services;

public interface IChatService
{
    Task<ChatSessionDto?> GetSessionAsync(int sessionId);
    Task<IEnumerable<ChatSessionDto>> GetUserSessionsAsync(int userId);
    Task<ChatSessionDto> CreateSessionAsync(CreateChatSessionDto dto, int userId);
    Task<IEnumerable<ChatMessageDto>> GetMessagesAsync(int sessionId);
    Task<ChatResponseDto> SendMessageAsync(SendMessageDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteSessionAsync(int sessionId, int userId);
}

public class ChatService : IChatService
{
    private readonly IUnitOfWork _uow;
    private readonly EmbeddingProviderFactory _embeddingFactory;
    private readonly ILogger<ChatService> _logger;
    private readonly ApiKeysSettings _apiKeys;

    private const int MaxHistoryMessages = 10; // context window limit

    public ChatService(
        IUnitOfWork uow,
        EmbeddingProviderFactory embeddingFactory,
        ILogger<ChatService> logger,
        ApiKeysSettings apiKeys)
    {
        _uow = uow;
        _embeddingFactory = embeddingFactory;
        _logger = logger;
        _apiKeys = apiKeys;
    }

    // ── Session Management ────────────────────────────────────────────

    public async Task<ChatSessionDto?> GetSessionAsync(int sessionId)
    {
        return await _uow.ChatSessions.Query()
            .Where(s => s.ChatSessionId == sessionId)
            .Select(s => new ChatSessionDto
            {
                ChatSessionId = s.ChatSessionId,
                SessionTitle = s.SessionTitle,
                SubjectName = s.Subject.SubjectName,
                AiModelName = s.AiModel.ModelName,
                MessageCount = s.ChatHistories.Count,
                CreatedAt = s.CreatedAt,
                LastActivityAt = s.LastActivityAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ChatSessionDto>> GetUserSessionsAsync(int userId)
    {
        return await _uow.ChatSessions.Query()
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.LastActivityAt ?? s.CreatedAt)
            .Select(s => new ChatSessionDto
            {
                ChatSessionId = s.ChatSessionId,
                SessionTitle = s.SessionTitle,
                SubjectName = s.Subject.SubjectName,
                AiModelName = s.AiModel.ModelName,
                MessageCount = s.ChatHistories.Count,
                CreatedAt = s.CreatedAt,
                LastActivityAt = s.LastActivityAt
            })
            .ToListAsync();
    }

    public async Task<ChatSessionDto> CreateSessionAsync(CreateChatSessionDto dto, int userId)
    {
        var session = new ChatSession
        {
            UserId = userId,
            SubjectId = dto.SubjectId,
            AiModelId = dto.AiModelId,
            EmbeddingModelId = dto.EmbeddingModelId,
            SessionTitle = dto.SessionTitle,
            TopK = dto.TopK,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        await _uow.ChatSessions.AddAsync(session);
        await _uow.SaveChangesAsync();

        return await GetSessionAsync(session.ChatSessionId)
            ?? throw new Exception("Session not found after creation");
    }

    public async Task<IEnumerable<ChatMessageDto>> GetMessagesAsync(int sessionId)
    {
        var histories = await _uow.ChatHistories.Query()
            .Where(h => h.ChatSessionId == sessionId)
            .OrderBy(h => h.CreatedAt)
            .Include(h => h.Citations)
                .ThenInclude(c => c.DocumentChunk)
                    .ThenInclude(dc => dc.Document)
                        .ThenInclude(d => d.Chapter)
            .ToListAsync();

        return histories.Select(h => new ChatMessageDto
        {
            ChatHistoryId = h.ChatHistoryId,
            Role = h.Role,
            Content = h.Content,
            LatencyMs = h.LatencyMs,
            CreatedAt = h.CreatedAt,
            Citations = h.Citations.Select(c => new CitationDto
            {
                DocumentChunkId = c.DocumentChunkId,
                ChunkText = c.DocumentChunk.ChunkText,
                DocumentName = c.DocumentChunk.Document.OriginalFileName ?? c.DocumentChunk.Document.FileName,
                ChapterName = c.DocumentChunk.Document.Chapter.ChapterName,
                SimilarityScore = c.SimilarityScore,
                RetrievalRank = c.RetrievalRank
            }).ToList()
        });
    }

    // ── Main RAG Chat ─────────────────────────────────────────────────

    public async Task<ChatResponseDto> SendMessageAsync(
        SendMessageDto dto,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Load session
            var session = await _uow.ChatSessions.Query()
                .Include(s => s.AiModel)
                .Include(s => s.EmbeddingModel)
                .FirstOrDefaultAsync(s => s.ChatSessionId == dto.ChatSessionId)
                ?? throw new Exception("Chat session not found");

            // 1. Embed the user question
            var embeddingProvider = _embeddingFactory.Create(session.EmbeddingModel);
            float[] questionVector;
            try
            {
                questionVector = await embeddingProvider.EmbedAsync(dto.Question, cancellationToken);
            }
            catch (RateLimitException)
            {
                return new ChatResponseDto
                {
                    IsError = true,
                    ErrorMessage = "⚠️ Embedding API rate limit reached. Please wait a moment and try again."
                };
            }

            // 2. Retrieve all indexed chunks for this subject
            var subjectId = session.SubjectId;
            var allChunks = await _uow.DocumentChunks.Query()
                .Where(c => c.Document.Chapter.SubjectId == subjectId
                         && c.EmbeddingModelId == session.EmbeddingModelId
                         && c.Document.Status == "Indexed")
                .Select(c => new
                {
                    c.DocumentChunkId,
                    c.ChunkText,
                    c.EmbeddingJson,
                    c.Document.OriginalFileName,
                    ChapterName = c.Document.Chapter.ChapterName
                })
                .ToListAsync();

            if (allChunks.Count == 0)
            {
                return new ChatResponseDto
                {
                    IsError = true,
                    ErrorMessage = "⚠️ No indexed documents found for this subject. Please upload and index documents first."
                };
            }

            // 3. Compute cosine similarity → Top-K chunks
            var chunkVectors = allChunks
                .Select(c => VectorHelper.DeserializeEmbedding(c.EmbeddingJson))
                .ToList();

            var topK = VectorHelper.TopKSimilar(questionVector, chunkVectors, session.TopK);
            var retrievedChunks = topK
                .Select(r => new
                {
                    allChunks[r.Index].DocumentChunkId,
                    allChunks[r.Index].ChunkText,
                    allChunks[r.Index].OriginalFileName,
                    allChunks[r.Index].ChapterName,
                    SimilarityScore = r.Score,
                    Rank = r.Index
                })
                .ToList();

            // 4. Load recent conversation history for context
            var history = await _uow.ChatHistories.Query()
                .Where(h => h.ChatSessionId == dto.ChatSessionId)
                .OrderByDescending(h => h.CreatedAt)
                .Take(MaxHistoryMessages)
                .OrderBy(h => h.CreatedAt)
                .Select(h => new { h.Role, h.Content })
                .ToListAsync();

            // 5. Build Gemini prompt
            var contextBuilder = new StringBuilder();
            for (int i = 0; i < retrievedChunks.Count; i++)
            {
                var chunk = retrievedChunks[i];
                contextBuilder.AppendLine($"[Context {i + 1}] (from '{chunk.ChapterName}' — {chunk.OriginalFileName}):");
                contextBuilder.AppendLine(chunk.ChunkText);
                contextBuilder.AppendLine();
            }

            var systemPrompt = $"""
                You are an intelligent learning assistant for a course management system.
                Answer the student's question based ONLY on the provided context below.
                If the context doesn't contain enough information, say so clearly.
                Always cite which context number(s) you used.
                Be concise, accurate, and educational.

                === CONTEXT ===
                {contextBuilder}
                === END CONTEXT ===
                """;

            // 6. Call Gemini API with retry on 429
            var answer = await CallGeminiWithRetryAsync(
                session.AiModel.ModelName,
                systemPrompt,
                history.Select(h => (h.Role, h.Content)).ToList(),
                dto.Question,
                cancellationToken);

            stopwatch.Stop();

            // 7. Save user message to history
            var userMessage = new ChatHistory
            {
                ChatSessionId = dto.ChatSessionId,
                Role = "user",
                Content = dto.Question,
                TokenCount = dto.Question.Split(' ').Length,
                CreatedAt = DateTime.UtcNow
            };
            await _uow.ChatHistories.AddAsync(userMessage);
            await _uow.SaveChangesAsync();

            // 8. Save assistant response + citations
            var assistantMessage = new ChatHistory
            {
                ChatSessionId = dto.ChatSessionId,
                Role = "assistant",
                Content = answer,
                TokenCount = answer.Split(' ').Length,
                LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                CreatedAt = DateTime.UtcNow
            };
            await _uow.ChatHistories.AddAsync(assistantMessage);
            await _uow.SaveChangesAsync();

            // Save citations
            var citations = retrievedChunks
                .Select((c, idx) => new ChatCitation
                {
                    ChatHistoryId = assistantMessage.ChatHistoryId,
                    DocumentChunkId = c.DocumentChunkId,
                    SimilarityScore = c.SimilarityScore,
                    RetrievalRank = idx + 1
                })
                .ToList();

            await _uow.ChatCitations.AddRangeAsync(citations);

            // Update session last activity
            var sessionEntity = await _uow.ChatSessions.GetByIdAsync(dto.ChatSessionId);
            if (sessionEntity != null)
            {
                sessionEntity.LastActivityAt = DateTime.UtcNow;
                _uow.ChatSessions.Update(sessionEntity);
            }
            await _uow.SaveChangesAsync();

            return new ChatResponseDto
            {
                Answer = answer,
                LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                Citations = retrievedChunks.Select((c, idx) => new CitationDto
                {
                    DocumentChunkId = c.DocumentChunkId,
                    ChunkText = c.ChunkText,
                    DocumentName = c.OriginalFileName ?? "Unknown",
                    ChapterName = c.ChapterName,
                    SimilarityScore = c.SimilarityScore,
                    RetrievalRank = idx + 1
                }).ToList()
            };
        }
        catch (OperationCanceledException)
        {
            return new ChatResponseDto { IsError = true, ErrorMessage = "Request was cancelled." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message for session {SessionId}", dto.ChatSessionId);
            return new ChatResponseDto { IsError = true, ErrorMessage = $"An error occurred: {ex.Message}" };
        }
    }

    // ── Gemini Chat API Call with 429 Retry ──────────────────────────

    private async Task<string> CallGeminiWithRetryAsync(
        string modelName,
        string systemPrompt,
        List<(string Role, string Content)> history,
        string userQuestion,
        CancellationToken cancellationToken)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={_apiKeys.Gemini}";

        // Build contents array (history + current question)
        var contents = new List<object>();
        foreach (var (role, content) in history)
        {
            var geminiRole = role == "assistant" ? "model" : "user";
            contents.Add(new { role = geminiRole, parts = new[] { new { text = content } } });
        }
        contents.Add(new { role = "user", parts = new[] { new { text = userQuestion } } });

        var payload = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents
        };

        using var httpClient = new HttpClient();
        int attempt = 0;
        const int maxRetries = 5;

        while (true)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = await httpClient.PostAsync(url, content, cancellationToken);

                // Handle 429 Rate Limit
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    attempt++;
                    if (attempt >= maxRetries)
                        throw new RateLimitException("Gemini chat API rate limit exceeded after max retries");

                    // Check Retry-After header from Gemini
                    int delayMs = 2000 * (int)Math.Pow(2, attempt - 1); // 2s, 4s, 8s, 16s, 32s
                    if (response.Headers.TryGetValues("Retry-After", out var values)
                        && int.TryParse(values.FirstOrDefault(), out int retryAfter))
                        delayMs = Math.Max(delayMs, retryAfter * 1000);

                    _logger.LogWarning("Gemini API rate limit (429) hit. Retry attempt {Attempt}/{MaxRetries} waiting {Delay}ms before retrying.", attempt, maxRetries, delayMs);
                    await Task.Delay(delayMs, cancellationToken);
                    continue;
                }

                // Handle 503 Service Unavailable (overloaded)
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    attempt++;
                    if (attempt >= maxRetries) 
                    {
                        _logger.LogError("Gemini API is unavailable after {MaxRetries} attempts.", maxRetries);
                        throw new Exception("Gemini API unavailable");
                    }
                    var retryDelay = 3000 * attempt;
                    _logger.LogWarning("Gemini API service unavailable (503). Retrying attempt {Attempt}/{MaxRetries} in {Delay}ms.", attempt, maxRetries, retryDelay);
                    await Task.Delay(retryDelay, cancellationToken);
                    continue;
                }

                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return ParseGeminiResponse(body);
            }
            catch (RateLimitException) { throw; }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                attempt++;
                if (attempt >= maxRetries) throw;
                await Task.Delay(2000 * (int)Math.Pow(2, attempt), cancellationToken);
            }
        }
    }

    private static string ParseGeminiResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "No response generated.";
    }

    public async Task<bool> DeleteSessionAsync(int sessionId, int userId)
    {
        var session = await _uow.ChatSessions
            .FirstOrDefaultAsync(s => s.ChatSessionId == sessionId && s.UserId == userId);
        if (session is null) return false;

        session.IsActive = false;
        _uow.ChatSessions.Update(session);
        await _uow.SaveChangesAsync();
        return true;
    }
}
