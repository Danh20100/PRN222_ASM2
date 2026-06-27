namespace BusinessLayer.Interfaces;

public interface IDocumentRealtimeNotifier
{
    Task NotifyDocumentUpdateAsync(
        string action,
        int documentId,
        string? status = null,
        CancellationToken cancellationToken = default);
}
