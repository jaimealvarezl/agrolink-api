using AgroLink.Domain.Entities;

namespace AgroLink.Application.Interfaces;

public interface IVoiceCommandJobRepository
{
    Task AddAsync(VoiceCommandJob job, CancellationToken ct = default);
    void Update(VoiceCommandJob job);
    Task<VoiceCommandJob?> GetByIdAsync(Guid jobId, CancellationToken ct = default);
    Task<int> DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default);
}
