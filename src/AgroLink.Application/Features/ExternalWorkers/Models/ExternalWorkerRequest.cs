using System.Text.Json;

namespace AgroLink.Application.Features.ExternalWorkers.Models;

public record ExternalWorkerRequest(string CorrelationId, string Operation, JsonElement Payload);
