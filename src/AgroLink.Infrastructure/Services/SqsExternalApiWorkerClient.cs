using System.Text.Json;
using AgroLink.Application.Features.ExternalWorkers.Models;
using AgroLink.Application.Interfaces;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgroLink.Infrastructure.Services;

public class SqsExternalApiWorkerClient(
    IAmazonSQS sqs,
    IConfiguration configuration,
    ILogger<SqsExternalApiWorkerClient> logger
) : IExternalApiWorkerClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _requestsQueueUrl =
        configuration["ExternalWorkers:RequestsQueueUrl"]
        ?? throw new InvalidOperationException(
            "ExternalWorkers:RequestsQueueUrl configuration is missing."
        );

    private readonly string _resultsQueueUrl =
        configuration["ExternalWorkers:ResultsQueueUrl"]
        ?? throw new InvalidOperationException(
            "ExternalWorkers:ResultsQueueUrl configuration is missing."
        );

    public async Task<ExternalWorkerResponse> ExecuteAsync(
        ExternalWorkerRequest request,
        CancellationToken ct
    )
    {
        var body = JsonSerializer.Serialize(request, JsonOptions);
        await sqs.SendMessageAsync(
            new SendMessageRequest { QueueUrl = _requestsQueueUrl, MessageBody = body },
            ct
        );

        logger.LogDebug(
            "Dispatched external worker operation {Operation} with correlation {CorrelationId}",
            request.Operation,
            request.CorrelationId
        );

        return await PollForResultAsync(request.CorrelationId, ct);
    }

    private async Task<ExternalWorkerResponse> PollForResultAsync(
        string correlationId,
        CancellationToken ct
    )
    {
        var deadline = DateTime.UtcNow.AddSeconds(120);

        while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
        {
            var receiveResponse = await sqs.ReceiveMessageAsync(
                new ReceiveMessageRequest
                {
                    QueueUrl = _resultsQueueUrl,
                    WaitTimeSeconds = 10,
                    MaxNumberOfMessages = 10,
                },
                ct
            );

            foreach (var msg in receiveResponse.Messages)
            {
                var result = JsonSerializer.Deserialize<ExternalWorkerResponse>(
                    msg.Body,
                    JsonOptions
                );

                if (result?.CorrelationId == correlationId)
                {
                    await sqs.DeleteMessageAsync(_resultsQueueUrl, msg.ReceiptHandle, ct);
                    logger.LogDebug(
                        "Received result for correlation {CorrelationId}. Success: {Success}",
                        correlationId,
                        result.Success
                    );
                    return result;
                }

                // Not ours — make it immediately visible for other consumers
                await sqs.ChangeMessageVisibilityAsync(_resultsQueueUrl, msg.ReceiptHandle, 0, ct);
            }
        }

        throw new TimeoutException(
            $"No result received for correlation '{correlationId}' within the timeout window."
        );
    }
}
