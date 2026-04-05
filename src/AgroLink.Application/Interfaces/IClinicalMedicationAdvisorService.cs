using AgroLink.Application.Features.ClinicalCases.Models;

namespace AgroLink.Application.Interfaces;

public interface IClinicalMedicationAdvisorService
{
    Task<ClinicalMedicationAdviceResult> GetAdviceAsync(
        ClinicalMedicationAdviceRequest request,
        CancellationToken ct = default
    );
}
