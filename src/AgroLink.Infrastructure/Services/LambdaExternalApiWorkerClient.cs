using System.Text.Json;
using AgroLink.Application.Features.ExternalWorkers.Models;
using AgroLink.Application.Interfaces;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgroLink.Infrastructure.Services;

public class LambdaExternalApiWorkerClient(
    IAmazonLambda lambda,
    IConfiguration configuration,
    ILogger<LambdaExternalApiWorkerClient> logger
) : IExternalApiWorkerClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _functionName =
        configuration["ExternalWorkers:WorkerFunctionName"]
        ?? throw new InvalidOperationException(
            "ExternalWorkers:WorkerFunctionName configuration is missing."
        );

    public async Task<ExternalWorkerResponse> ExecuteAsync(
        ExternalWorkerRequest request,
        CancellationToken ct
    )
    {
        var payload = JsonSerializer.Serialize(request, JsonOptions);

        logger.LogDebug(
            "Invoking Lambda {FunctionName} for operation {Operation} with correlation {CorrelationId}",
            _functionName,
            request.Operation,
            request.CorrelationId
        );

        var invokeResponse = await lambda.InvokeAsync(
            new InvokeRequest
            {
                FunctionName = _functionName,
                InvocationType = InvocationType.RequestResponse,
                Payload = payload,
            },
            ct
        );

        if (!string.IsNullOrEmpty(invokeResponse.FunctionError))
        {
            using var errorReader = new StreamReader(invokeResponse.Payload);
            var errorBody = await errorReader.ReadToEndAsync(ct);
            throw new InvalidOperationException(
                $"Lambda function error for operation {request.Operation}: {invokeResponse.FunctionError}. Details: {errorBody}"
            );
        }

        using var reader = new StreamReader(invokeResponse.Payload);
        var responseBody = await reader.ReadToEndAsync(ct);

        logger.LogDebug(
            "Received Lambda response for operation {Operation}, correlation {CorrelationId}",
            request.Operation,
            request.CorrelationId
        );

        return JsonSerializer.Deserialize<ExternalWorkerResponse>(responseBody, JsonOptions)
            ?? throw new InvalidOperationException(
                $"Lambda returned null response for operation {request.Operation}."
            );
    }
}