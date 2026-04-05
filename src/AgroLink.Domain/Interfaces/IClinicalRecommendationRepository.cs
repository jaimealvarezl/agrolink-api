using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IClinicalRecommendationRepository : IRepository<ClinicalRecommendation>
{
    Task<ClinicalRecommendation?> GetLatestByCaseIdAsync(
        int caseId,
        CancellationToken ct = default
    );
}
