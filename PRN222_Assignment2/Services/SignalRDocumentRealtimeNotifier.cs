using BusinessLayer.DTOs;
using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.SignalR;
using PRN222_Assignment2.Hubs;

namespace PRN222_Assignment2.Services;

public class SignalRDocumentRealtimeNotifier : IDocumentRealtimeNotifier
{
    private readonly IHubContext<DocumentHub> _hubContext;

    public SignalRDocumentRealtimeNotifier(IHubContext<DocumentHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyDocumentUpdateAsync(
        string action,
        int documentId,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            DocumentId = documentId,
            Status = status,
            UpdatedAt = DateTime.UtcNow
        };

        return _hubContext.Clients.All.SendAsync("ReceiveDocumentUpdate", action, payload, cancellationToken);
    }

    public Task NotifyDocumentProgressAsync(
        DocumentProgressUpdate progress,
        CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients.All.SendAsync("ReceiveDocumentProgress", progress, cancellationToken);
    }
}
