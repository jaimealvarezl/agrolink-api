using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IClinicalCaseEventRepository : IRepository<ClinicalCaseEvent>
{
    Task<ClinicalCaseEvent?> GetLatestByCaseIdAsync(int caseId, CancellationToken ct = default);
}
