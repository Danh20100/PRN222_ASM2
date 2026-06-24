using System.Net;
using System.Text;
using System.Text.Json;
using BusinessLayer.Interfaces;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Services;

/// <summary>
/// HuggingFace Inference API embedding provider.
/// Compatible with sentence-transformers models.
/// </summary>
public class HuggingFaceEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelName;
    private readonly ILogger<HuggingFaceEmbeddingProvider> _logger;

    public string ProviderName => "HuggingFace";

    public HuggingFaceEmbeddingProvider(
        HttpClient httpClient,
        string apiKey,
        string modelName,
        ILogger<HuggingFaceEmbeddingProvider> logger)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _modelName = modelName;
        _logger = logger;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var results = await EmbedBatchAsync([text], cancellationToken);
        return results.First();
    }

    public async Task<List<float[]>> EmbedBatchAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://api-inference.huggingface.co/models/{_modelName}";
        var payload = new { inputs = texts.ToArray() };

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        int attempt = 0;
        while (true)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync(url, content, cancellationToken);

                if (response.StatusCode == HttpStatusCode.TooManyRequests || response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    attempt++;
                    if (attempt > 3) throw new RateLimitException($"HuggingFace rate limit: {response.StatusCode}");
                    int delay = 2000 * attempt;
                    _logger.LogWarning("HuggingFace rate limited. Retry {A} in {D}ms", attempt, delay);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return ParseBatchEmbeddings(body);
            }
            catch (RateLimitException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HuggingFace embedding error");
                throw;
            }
        }
    }

    private static List<float[]> ParseBatchEmbeddings(string json)
    {
        // HF returns: [[float, float, ...], [float, float, ...]]
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.EnumerateArray()
            .Select(arr => arr.EnumerateArray().Select(v => v.GetSingle()).ToArray())
            .ToList();
    }
}
