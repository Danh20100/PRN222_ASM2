using Microsoft.Extensions.Logging;

namespace BusinessLayer.Services;

public interface IFakeEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}

public class FakeEmailService : IFakeEmailService
{
    private readonly ILogger<FakeEmailService> _logger;
    private readonly string _emailFolder;

    public FakeEmailService(ILogger<FakeEmailService> logger)
    {
        _logger = logger;
        // Save emails to a local folder in the Web project root
        _emailFolder = Path.Combine(Directory.GetCurrentDirectory(), "FakeEmails");
        if (!Directory.Exists(_emailFolder))
        {
            Directory.CreateDirectory(_emailFolder);
        }
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{toEmail}.txt";
        var filePath = Path.Combine(_emailFolder, fileName);

        var content = $@"
========================================
TO: {toEmail}
SUBJECT: {subject}
DATE: {DateTime.Now}
========================================
{body}
========================================
";
        await File.WriteAllTextAsync(filePath, content);
        _logger.LogInformation("Fake email sent to {Email}. Check file: {FilePath}", toEmail, filePath);
    }
}
