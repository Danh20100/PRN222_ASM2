using System.Net;
using System.Text;
using System.Text.Json;
using BusinessLayer.Interfaces;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Services;

/// <summary>
/// Gemini Embedding Provider using text-embedding-004 model.
/// Implements exponential backoff for 429 Rate Limit and Quota errors.
/// </summary>
public class GeminiEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelName;
    private readonly ILogger<GeminiEmbeddingProvider> _logger;

    private const int MaxRetries = 5;
    private const int BaseDelayMs = 1000;

    public string ProviderName => "Gemini";

    public GeminiEmbeddingProvider(
        HttpClient httpClient,
        string apiKey,
        string modelName,
        ILogger<GeminiEmbeddingProvider> logger)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _modelName = modelName;
        _logger = logger;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:embedContent?key={_apiKey}";
        var payload = new
        {
            model = $"models/{_modelName}",
            content = new { parts = new[] { new { text } } }
        };

        return await ExecuteWithRetryAsync(async (ct) =>
        {
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(url, content, ct);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                throw new RateLimitException("Gemini API rate limit hit (429)");

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            return ParseEmbedding(responseBody);
        }, cancellationToken);
    }

    public async Task<List<float[]>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var result = new List<float[]>();
        // Gemini embedding API doesn't support native batch — sequential with delay
        foreach (var text in texts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.Add(await EmbedAsync(text, cancellationToken));

            // Small delay between calls to respect rate limits (1 RPM for free tier)
            await Task.Delay(200, cancellationToken);
        }
        return result;
    }

    private async Task<float[]> ExecuteWithRetryAsync(
        Func<CancellationToken, Task<float[]>> operation,
        CancellationToken cancellationToken)
    {
        int attempt = 0;
        while (true)
        {
            try
            {
                return await operation(cancellationToken);
            }
            catch (RateLimitException ex)
            {
                attempt++;
                if (attempt >= MaxRetries)
                {
                    _logger.LogError(ex, "Gemini rate limit exceeded after {Attempts} retries", attempt);
                    throw;
                }

                // Exponential backoff: 1s, 2s, 4s, 8s, 16s
                int delayMs = BaseDelayMs * (int)Math.Pow(2, attempt - 1);
                _logger.LogWarning(
                    "Gemini rate limit hit. Retry {Attempt}/{MaxRetries} in {Delay}ms",
                    attempt, MaxRetries, delayMs);

                await Task.Delay(delayMs, cancellationToken);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                attempt++;
                if (attempt >= MaxRetries) throw;
                int delayMs = BaseDelayMs * (int)Math.Pow(2, attempt - 1);
                _logger.LogWarning("Gemini 429 HttpRequestException. Retry {Attempt}/{Max} in {Delay}ms",
                    attempt, MaxRetries, delayMs);
                await Task.Delay(delayMs, cancellationToken);
            }
        }
    }

    private static float[] ParseEmbedding(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var values = doc.RootElement
            .GetProperty("embedding")
            .GetProperty("values");

        return values.EnumerateArray()
                     .Select(v => v.GetSingle())
                     .ToArray();
    }
}

/// <summary>Thrown when Gemini returns HTTP 429 Rate Limit</summary>
public class RateLimitException : Exception
{
    public RateLimitException(string message) : base(message) { }
}
