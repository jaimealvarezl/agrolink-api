namespace AgroLink.Application.Interfaces;

public interface IVoiceCommandQueue
{
    Task EnqueueAsync(Guid jobId, int farmId, int userId, CancellationToken ct = default);
}
