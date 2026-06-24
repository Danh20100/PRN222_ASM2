using System.Text;
using System.Text.Json;
using BusinessLayer.Interfaces;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Services;

/// <summary>OpenAI-compatible embedding provider (works with OpenAI and Azure OpenAI)</summary>
public class OpenAiEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelName;
    private readonly string _baseUrl;
    private readonly ILogger<OpenAiEmbeddingProvider> _logger;

    public string ProviderName => "OpenAI";

    public OpenAiEmbeddingProvider(
        HttpClient httpClient, string apiKey, string modelName,
        string baseUrl, ILogger<OpenAiEmbeddingProvider> logger)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _modelName = modelName;
        _baseUrl = baseUrl.TrimEnd('/');
        _logger = logger;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var results = await EmbedBatchAsync([text], cancellationToken);
        return results.First();
    }

    public async Task<List<float[]>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/embeddings";
        var payload = new { input = texts.ToArray(), model = _modelName };

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        int attempt = 0;
        while (true)
        {
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(url, content, cancellationToken);

            if ((int)response.StatusCode == 429)
            {
                attempt++;
                if (attempt > 4) throw new RateLimitException("OpenAI rate limit exceeded");
                int delay = 1000 * (int)Math.Pow(2, attempt);
                _logger.LogWarning("OpenAI 429. Retry {A} in {D}ms", attempt, delay);
                await Task.Delay(delay, cancellationToken);
                continue;
            }

            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("data")
                .EnumerateArray()
                .OrderBy(e => e.GetProperty("index").GetInt32())
                .Select(e => e.GetProperty("embedding").EnumerateArray()
                              .Select(v => v.GetSingle()).ToArray())
                .ToList();
        }
    }
}

/// <summary>Ollama local embedding provider (llama.cpp-compatible endpoint)</summary>
public class OllamaEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _modelName;
    private readonly string _baseUrl;
    private readonly ILogger<OllamaEmbeddingProvider> _logger;

    public string ProviderName => "Ollama";

    public OllamaEmbeddingProvider(
        HttpClient httpClient, string modelName,
        string baseUrl, ILogger<OllamaEmbeddingProvider> logger)
    {
        _httpClient = httpClient;
        _modelName = modelName;
        _baseUrl = baseUrl.TrimEnd('/');
        _logger = logger;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/embeddings";
        var payload = new { model = _modelName, prompt = text };
        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("embedding")
            .EnumerateArray()
            .Select(v => v.GetSingle())
            .ToArray();
    }

    public async Task<List<float[]>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var result = new List<float[]>();
        foreach (var text in texts)
        {
            result.Add(await EmbedAsync(text, cancellationToken));
            await Task.Delay(50, cancellationToken); // small throttle
        }
        return result;
    }
}
