using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class ClinicalCaseRepository(AgroLinkDbContext context)
    : Repository<ClinicalCase>(context),
        IClinicalCaseRepository
{
    public async Task<ClinicalCase?> GetOpenCaseByFarmAndAnimalWithinDaysAsync(
        int farmId,
        int animalId,
        int days,
        CancellationToken ct = default
    )
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Abs(days));

        return await _dbSet
            .Include(c => c.Events)
            .Include(c => c.Recommendations)
            .FirstOrDefaultAsync(
                c =>
                    c.FarmId == farmId
                    && c.AnimalId == animalId
                    && c.OpenedAt >= cutoff
                    && c.ClosedAt == null
                    && c.State != ClinicalCaseState.Closed,
                ct
            );
    }

    public async Task<ClinicalCase?> GetLatestByFarmAndAnimalAsync(
        int farmId,
        int animalId,
        CancellationToken ct = default
    )
    {
        return await _dbSet
            .Include(c => c.Events)
            .Include(c => c.Recommendations)
            .Where(c => c.FarmId == farmId && c.AnimalId == animalId)
            .OrderByDescending(c => c.OpenedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ClinicalCase?> GetOpenCaseByFarmAndReferenceWithinDaysAsync(
        int farmId,
        string? earTag,
        string? animalReference,
        int days,
        CancellationToken ct = default
    )
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Abs(days));
        var normalizedEarTag = earTag?.Trim().ToLowerInvariant();
        var normalizedAnimalReference = animalReference?.Trim().ToLowerInvariant();

        var query = _dbSet
            .Include(c => c.Events)
            .Include(c => c.Recommendations)
            .Where(c => c.FarmId == farmId && c.OpenedAt >= cutoff && c.ClosedAt == null);

        if (!string.IsNullOrWhiteSpace(normalizedEarTag))
        {
            query = query.Where(c => c.EarTag != null && c.EarTag.ToLower() == normalizedEarTag);
        }
        else if (!string.IsNullOrWhiteSpace(normalizedAnimalReference))
        {
            query = query.Where(c =>
                c.AnimalReferenceText != null
                && c.AnimalReferenceText.ToLower().Contains(normalizedAnimalReference)
            );
        }
        else
        {
            return null;
        }

        return await query.OrderByDescending(c => c.OpenedAt).FirstOrDefaultAsync(ct);
    }

    public async Task<ClinicalCase?> GetLatestByFarmAndReferenceAsync(
        int farmId,
        string? earTag,
        string? animalReference,
        CancellationToken ct = default
    )
    {
        var normalizedEarTag = earTag?.Trim().ToLowerInvariant();
        var normalizedAnimalReference = animalReference?.Trim().ToLowerInvariant();

        var query = _dbSet
            .Include(c => c.Events)
            .Include(c => c.Recommendations)
            .Where(c => c.FarmId == farmId);

        if (!string.IsNullOrWhiteSpace(normalizedEarTag))
        {
            query = query.Where(c => c.EarTag != null && c.EarTag.ToLower() == normalizedEarTag);
        }
        else if (!string.IsNullOrWhiteSpace(normalizedAnimalReference))
        {
            query = query.Where(c =>
                c.AnimalReferenceText != null
                && c.AnimalReferenceText.ToLower().Contains(normalizedAnimalReference)
            );
        }
        else
        {
            return null;
        }

        return await query.OrderByDescending(c => c.OpenedAt).FirstOrDefaultAsync(ct);
    }

    public async Task<ClinicalCase?> GetByIdWithDetailsAsync(
        int caseId,
        CancellationToken ct = default
    )
    {
        return await _dbSet
            .Include(c => c.Farm)
            .Include(c => c.Animal)
            .Include(c => c.Events)
            .Include(c => c.Recommendations)
            .Include(c => c.Alerts)
            .FirstOrDefaultAsync(c => c.Id == caseId, ct);
    }

    public async Task<IEnumerable<ClinicalCase>> GetByAnimalIdAsync(
        int animalId,
        CancellationToken ct = default
    )
    {
        return await _dbSet
            .Where(c => c.AnimalId == animalId)
            .OrderByDescending(c => c.OpenedAt)
            .ToListAsync(ct);
    }
}
