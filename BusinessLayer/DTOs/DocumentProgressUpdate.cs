namespace BusinessLayer.DTOs;

public class DocumentProgressUpdate
{
    public int DocumentId { get; set; }
    public int? ChapterId { get; set; }
    public int? SubjectId { get; set; }
    public string? FileName { get; set; }
    public string Step { get; set; } = string.Empty;
    public int Percent { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Status { get; set; }
    public int? TotalChunks { get; set; }
    public int? ProcessedChunks { get; set; }
    public string? ErrorMessage { get; set; }
}
