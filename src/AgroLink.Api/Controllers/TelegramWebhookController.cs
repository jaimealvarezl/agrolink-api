using System.Text;
using AgroLink.Application.Features.ClinicalCases.Commands.ReceiveTelegramUpdate;
using Amazon.SQS;
using Amazon.SQS.Model;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[ApiController]
[Route("api/integrations/telegram")]
public class TelegramWebhookController(
    IMediator mediator,
    IConfiguration configuration,
    IAmazonSQS sqsClient
) : ControllerBase
{
    [HttpPost("webhook")]
    public async Task<ActionResult<ReceiveTelegramUpdateResult>> ReceiveWebhook(
        [FromServices] ILogger<TelegramWebhookController> logger,
        CancellationToken cancellationToken
    )
    {
        var configuredSecret = configuration["Telegram:WebhookSecretToken"]?.Trim();
        if (!string.IsNullOrWhiteSpace(configuredSecret))
        {
            var incomingSecret = Request
                .Headers["X-Telegram-Bot-Api-Secret-Token"]
                .FirstOrDefault()
                ?.Trim();

            if (!string.Equals(configuredSecret, incomingSecret, StringComparison.Ordinal))
            {
                var headersList = string.Join(", ", Request.Headers.Keys);
                logger.LogWarning(
                    "Telegram webhook 401: Secret mismatch. Configured length: {ConfigLength}, Incoming length: {IncomingLength}. Received headers: {Headers}",
                    configuredSecret.Length,
                    incomingSecret?.Length ?? 0,
                    headersList
                );
                return Unauthorized("Invalid Telegram webhook secret.");
            }
        }

        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawPayload = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return BadRequest("Webhook payload is empty.");
        }

        var queueUrl = configuration["Telegram:SqsQueueUrl"];

        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            logger.LogWarning(
                "Telegram:SqsQueueUrl is not configured. Falling back to synchronous processing."
            );
            var syncResult = await mediator.Send(
                new ReceiveTelegramUpdateCommand(rawPayload),
                cancellationToken
            );
            return Ok(syncResult);
        }

        try
        {
            var sendMessageRequest = new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = rawPayload,
            };

            await sqsClient.SendMessageAsync(sendMessageRequest, cancellationToken);

            return Ok(
                new ReceiveTelegramUpdateResult
                {
                    Processed = true,
                    Status = "Queued",
                    Message = "Update received and queued for background processing.",
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error pushing Telegram update to SQS queue {QueueUrl}", queueUrl);

            // Fallback to sync processing if SQS fails to avoid losing the update
            var fallbackResult = await mediator.Send(
                new ReceiveTelegramUpdateCommand(rawPayload),
                cancellationToken
            );
            return Ok(fallbackResult);
        }
    }
}
