using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class ClinicalRecommendationRepository(AgroLinkDbContext context)
    : Repository<ClinicalRecommendation>(context),
        IClinicalRecommendationRepository
{
    public async Task<ClinicalRecommendation?> GetLatestByCaseIdAsync(
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
