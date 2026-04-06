using AgroLink.Application.Features.ExternalWorkers.Models;

namespace AgroLink.Application.Interfaces;

public interface IExternalApiWorkerClient
{
    Task<ExternalWorkerResponse> ExecuteAsync(ExternalWorkerRequest request, CancellationToken ct);
}
