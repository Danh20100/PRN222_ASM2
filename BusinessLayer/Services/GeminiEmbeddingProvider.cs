using System.Net;
using System.Text;
using System.Text.Json;
using BusinessLayer.Interfaces;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Services;

/// <summary>
/// Gemini Embedding Provider — uses gemini-embedding-001 (text-embedding-004 was retired).
/// </summary>
public class GeminiEmbeddingProvider : IEmbeddingProvider
{
    private static readonly HashSet<string> DeprecatedModels = new(StringComparer.OrdinalIgnoreCase)
    {
        "text-embedding-004",
        "embedding-001"
    };

    private const string DefaultModel = "gemini-embedding-001";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelName;
    private readonly int _outputDimensionality;
    private readonly ILogger<GeminiEmbeddingProvider> _logger;

    private const int MaxRetries = 5;
    private const int BaseDelayMs = 1000;

    public string ProviderName => "Gemini";

    public GeminiEmbeddingProvider(
        HttpClient httpClient,
        string apiKey,
        string modelName,
        ILogger<GeminiEmbeddingProvider> logger,
        int outputDimensionality = 768)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _modelName = NormalizeModelName(modelName);
        _outputDimensionality = outputDimensionality;
        _logger = logger;

        if (_modelName != modelName)
        {
            _logger.LogWarning(
                "Gemini embedding model '{OldModel}' is deprecated; using '{NewModel}' instead.",
                modelName, _modelName);
        }
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:embedContent?key={_apiKey}";
        var payload = new
        {
            taskType = "RETRIEVAL_DOCUMENT",
            outputDimensionality = _outputDimensionality,
            content = new { parts = new[] { new { text } } }
        };

        return await ExecuteWithRetryAsync(async (ct) =>
        {
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(url, content, ct);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                throw new RateLimitException("Gemini API rate limit hit (429)");

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError(
                    "Gemini embedContent failed ({Status}): {Body}",
                    (int)response.StatusCode, errorBody);
                throw new HttpRequestException(
                    $"Gemini embedding API error {(int)response.StatusCode}: {ExtractErrorMessage(errorBody)}",
                    null,
                    response.StatusCode);
            }

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var embedding = ParseEmbedding(responseBody);

            if (_modelName == DefaultModel && _outputDimensionality < 3072)
                return NormalizeVector(embedding);

            return embedding;
        }, cancellationToken);
    }

    public async Task<List<float[]>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var result = new List<float[]>();
        foreach (var text in texts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.Add(await EmbedAsync(text, cancellationToken));
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
                await Task.Delay(delayMs, cancellationToken);
            }
        }
    }

    private static string NormalizeModelName(string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName) || DeprecatedModels.Contains(modelName))
            return DefaultModel;
        return modelName.Trim();
    }

    private static float[] NormalizeVector(float[] vector)
    {
        double magnitude = Math.Sqrt(vector.Sum(v => (double)v * v));
        if (magnitude <= 0)
            return vector;

        return vector.Select(v => (float)(v / magnitude)).ToArray();
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

    private static string ExtractErrorMessage(string errorBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(errorBody);
            if (doc.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? errorBody;
            }
        }
        catch
        {
            // ignore parse errors
        }

        return string.IsNullOrWhiteSpace(errorBody) ? "Unknown error" : errorBody;
    }
}

/// <summary>Thrown when Gemini returns HTTP 429 Rate Limit</summary>
public class RateLimitException : Exception
{
    public RateLimitException(string message) : base(message) { }
}
