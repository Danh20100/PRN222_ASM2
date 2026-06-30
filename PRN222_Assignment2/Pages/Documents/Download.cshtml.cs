using DataAccessLayer.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PRN222_Assignment2.Pages.Documents;

[Authorize]
public class DownloadModel : PageModel
{
    private readonly IUnitOfWork _uow;

    public DownloadModel(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var doc = await _uow.Documents.GetByIdAsync(id);
        if (doc?.StoragePath == null || !System.IO.File.Exists(doc.StoragePath))
            return NotFound();

        var contentType = GetContentType(doc.FileType);
        var downloadName = doc.OriginalFileName ?? doc.FileName;

        return PhysicalFile(doc.StoragePath, contentType, downloadName);
    }

    private static string GetContentType(string? fileType)
    {
        return (fileType ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant() switch
        {
            "pdf" => "application/pdf",
            "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "doc" => "application/msword",
            "txt" => "text/plain",
            "md" => "text/markdown",
            _ => "application/octet-stream"
        };
    }
}
