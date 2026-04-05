using AgroLink.Application.Features.ClinicalCases.Models;

namespace AgroLink.Application.Interfaces;

public interface IClinicalExtractionService
{
    Task<ClinicalExtractionResult> ExtractAsync(string messageText, CancellationToken ct = default);
}
