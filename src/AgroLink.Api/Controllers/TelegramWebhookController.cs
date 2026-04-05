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
        CancellationToken cancellationToken
    )
    {
        var configuredSecret = configuration["Telegram:WebhookSecretToken"];
        if (!string.IsNullOrWhiteSpace(configuredSecret))
        {
            var incomingSecret = Request
                .Headers["X-Telegram-Bot-Api-Secret-Token"]
                .FirstOrDefault();
            if (!string.Equals(configuredSecret, incomingSecret, StringComparison.Ordinal))
            {
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
