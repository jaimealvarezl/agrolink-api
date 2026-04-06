using System.Text.Json;
using AgroLink.Application.Features.ClinicalCases.Models;
using AgroLink.Application.Features.ExternalWorkers.Models;
using AgroLink.Application.Interfaces;

namespace AgroLink.Infrastructure.Services;

public class SqsClinicalMedicationAdvisorService(IExternalApiWorkerClient client)
    : IClinicalMedicationAdvisorService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<ClinicalMedicationAdviceResult> GetAdviceAsync(
        ClinicalMedicationAdviceRequest request,
        CancellationToken ct
    )
    {
        var workerRequest = new ExternalWorkerRequest(
            Guid.NewGuid().ToString(),
            ExternalWorkerOperations.GetMedicationAdvice,
            JsonSerializer.SerializeToElement(request, JsonOptions)
        );

        var response = await client.ExecuteAsync(workerRequest, ct);

        if (!response.Success)
        {
            throw new InvalidOperationException($"GetMedicationAdvice failed: {response.Error}");
        }

        return response.Result?.Deserialize<ClinicalMedicationAdviceResult>(JsonOptions)
            ?? throw new InvalidOperationException("GetMedicationAdvice returned a null result.");
    }
}
