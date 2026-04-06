using System.Text;
using AgroLink.Application.Features.ClinicalCases.Commands.ReceiveTelegramUpdate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[ApiController]
[Route("api/integrations/telegram")]
public class TelegramWebhookController(IMediator mediator, IConfiguration configuration)
    : ControllerBase
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
        var rawPayload = await reader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return BadRequest("Webhook payload is empty.");
        }

        var result = await mediator.Send(
            new ReceiveTelegramUpdateCommand(rawPayload),
            cancellationToken
        );
        return Ok(result);
    }
}
