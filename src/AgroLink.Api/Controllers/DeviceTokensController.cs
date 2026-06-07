using AgroLink.Application.Features.Notifications.Commands.DeleteDeviceToken;
using AgroLink.Application.Features.Notifications.Commands.RegisterDeviceToken;
using AgroLink.Application.Features.Notifications.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Authorize]
[Route("api/me/device-tokens")]
public class DeviceTokensController(IMediator mediator) : BaseController
{
    [HttpPost]
    public async Task<ActionResult<DeviceTokenDto>> Register(
        [FromBody] RegisterDeviceTokenRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(
            new RegisterDeviceTokenCommand(request.Token, request.Platform, GetCurrentUserId()),
            cancellationToken
        );

        return Ok(result);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(
        [FromQuery] string token,
        CancellationToken cancellationToken
    )
    {
        await mediator.Send(
            new DeleteDeviceTokenCommand(token, GetCurrentUserId()),
            cancellationToken
        );
        return NoContent();
    }
}
