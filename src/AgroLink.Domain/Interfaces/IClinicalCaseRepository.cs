using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IClinicalCaseRepository : IRepository<ClinicalCase>
{
    Task<ClinicalCase?> GetOpenCaseByFarmAndAnimalWithinDaysAsync(
        int farmId,
        int animalId,
        int days,
        CancellationToken ct = default
    );

    Task<ClinicalCase?> GetLatestByFarmAndAnimalAsync(
        int farmId,
        int animalId,
        CancellationToken ct = default
    );

    Task<ClinicalCase?> GetOpenCaseByFarmAndReferenceWithinDaysAsync(
        int farmId,
        string? earTag,
        string? animalReference,
        int days,
        CancellationToken ct = default
    );

    Task<ClinicalCase?> GetLatestByFarmAndReferenceAsync(
        int farmId,
        string? earTag,
        string? animalReference,
        CancellationToken ct = default
    );

    Task<ClinicalCase?> GetByIdWithDetailsAsync(int caseId, CancellationToken ct = default);

    Task<IEnumerable<ClinicalCase>> GetByAnimalIdAsync(
        int animalId,
        CancellationToken ct = default
    );
}
