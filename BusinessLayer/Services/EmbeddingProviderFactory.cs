using BusinessLayer.Interfaces;
using BusinessLayer.Models;
using DataAccessLayer.Entities;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Services;

/// <summary>
/// Factory Pattern: creates the correct IEmbeddingProvider based on
/// the EmbeddingModel.Provider value from DB.
/// BusinessLayer uses plain HttpClient — DI wires IHttpClientFactory in the Web project.
/// </summary>
public class EmbeddingProviderFactory
{
    private readonly Func<HttpClient> _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ApiKeysSettings _apiKeys;

    public EmbeddingProviderFactory(
        Func<HttpClient> httpClientFactory,
        ILoggerFactory loggerFactory,
        ApiKeysSettings apiKeys)
    {
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _apiKeys = apiKeys;
    }

    /// <summary>
    /// Create the appropriate provider based on EmbeddingModel.Provider from DB.
    /// </summary>
    public IEmbeddingProvider Create(EmbeddingModel model)
    {
        var httpClient = _httpClientFactory();

        return model.Provider switch
        {
            "Gemini" => new GeminiEmbeddingProvider(
                httpClient,
                _apiKeys.Gemini,
                model.ModelName,
                _loggerFactory.CreateLogger<GeminiEmbeddingProvider>(),
                model.VectorDimension > 0 ? model.VectorDimension : 768),

            "HuggingFace" => new HuggingFaceEmbeddingProvider(
                httpClient,
                _apiKeys.HuggingFace,
                model.ModelName,
                _loggerFactory.CreateLogger<HuggingFaceEmbeddingProvider>()),

            "OpenAI" => new OpenAiEmbeddingProvider(
                httpClient,
                _apiKeys.OpenAI,
                model.ModelName,
                model.ApiEndpoint ?? "https://api.openai.com/v1",
                _loggerFactory.CreateLogger<OpenAiEmbeddingProvider>()),

            "Ollama" => new OllamaEmbeddingProvider(
                httpClient,
                model.ModelName,
                model.ApiEndpoint ?? _apiKeys.OllamaBaseUrl,
                _loggerFactory.CreateLogger<OllamaEmbeddingProvider>()),

            _ => throw new NotSupportedException($"Embedding provider '{model.Provider}' is not supported.")
        };
    }
}
