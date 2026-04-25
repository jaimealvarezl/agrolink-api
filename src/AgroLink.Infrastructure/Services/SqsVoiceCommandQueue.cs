using System.Text.Json;
using AgroLink.Application.Interfaces;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgroLink.Infrastructure.Services;

public class SqsVoiceCommandQueue(
    IAmazonSQS sqsClient,
    IConfiguration configuration,
    ILogger<SqsVoiceCommandQueue> logger
) : IVoiceCommandQueue
{
    public async Task EnqueueAsync(
        Guid jobId,
        int farmId,
        int userId,
        CancellationToken ct = default
    )
    {
        var queueUrl = configuration["VoiceCommands:SqsQueueUrl"];
        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            logger.LogWarning(
                "VoiceCommands:SqsQueueUrl is not configured. Skipping SQS enqueue for job {JobId}.",
                jobId
            );
            return;
        }

        var body = JsonSerializer.Serialize(
            new
            {
                jobId,
                farmId,
                userId,
            }
        );
        await sqsClient.SendMessageAsync(
            new SendMessageRequest { QueueUrl = queueUrl, MessageBody = body },
            ct
        );
    }
}
