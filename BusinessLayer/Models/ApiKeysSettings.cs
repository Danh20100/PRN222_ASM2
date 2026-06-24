namespace BusinessLayer.Models;

/// <summary>
/// Strongly-typed settings class cho section "ApiKeys" trong appsettings.json.
/// Đây là BL Model dạng Config/Settings — không ánh xạ DB, không phải DTO.
/// Program.cs bind từ IConfiguration rồi truyền vào service qua constructor.
/// </summary>
public class ApiKeysSettings
{
    /// <summary>Tên section trong appsettings.json</summary>
    public const string SectionName = "ApiKeys";

    /// <summary>Gemini API Key — dùng cho Gemini Embedding và Gemini Chat (generateContent)</summary>
    public string Gemini { get; set; } = string.Empty;

    /// <summary>HuggingFace Inference API Token — dùng cho HuggingFace Embedding Provider</summary>
    public string HuggingFace { get; set; } = string.Empty;

    /// <summary>OpenAI API Key — dùng cho OpenAI-compatible Embedding Provider</summary>
    public string OpenAI { get; set; } = string.Empty;

    /// <summary>Base URL của Ollama local server — mặc định http://localhost:11434</summary>
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
}
