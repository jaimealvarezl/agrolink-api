using System.Text.Json;

namespace AgroLink.Application.Features.ExternalWorkers.Models;

public record ExternalWorkerResponse(
    string CorrelationId,
    string Operation,
    bool Success,
    JsonElement? Result,
    string? Error
);
