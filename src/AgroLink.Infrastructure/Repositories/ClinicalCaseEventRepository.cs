using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class ClinicalCaseEventRepository(AgroLinkDbContext context)
    : Repository<ClinicalCaseEvent>(context),
        IClinicalCaseEventRepository
{
    public async Task<ClinicalCaseEvent?> GetLatestByCaseIdAsync(
        int caseId,
        CancellationToken ct = default
    )
    {
        return await _dbSet
            .Where(x => x.ClinicalCaseId == caseId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }
}
