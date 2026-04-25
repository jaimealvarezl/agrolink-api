using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class VoiceCommandJobRepository(AgroLinkDbContext context) : IVoiceCommandJobRepository
{
    public async Task AddAsync(VoiceCommandJob job, CancellationToken ct = default)
    {
        await context.VoiceCommandJobs.AddAsync(job, ct);
    }

    public async Task<VoiceCommandJob?> GetByIdAsync(Guid jobId, CancellationToken ct = default)
    {
        return await context
            .VoiceCommandJobs.AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);
    }

    public async Task<int> DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default)
    {
        return await context
            .VoiceCommandJobs.Where(j => j.CreatedAt < cutoff)
            .ExecuteDeleteAsync(ct);
    }
}
