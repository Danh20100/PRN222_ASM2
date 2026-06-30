using BusinessLayer.DTOs;

namespace BusinessLayer.Interfaces;

public interface IDocumentRealtimeNotifier
{
    Task NotifyDocumentUpdateAsync(
        string action,
        int documentId,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task NotifyDocumentProgressAsync(
        DocumentProgressUpdate progress,
        CancellationToken cancellationToken = default);
}
