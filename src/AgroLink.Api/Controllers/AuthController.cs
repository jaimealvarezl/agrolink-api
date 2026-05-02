using AgroLink.Application.Features.Auth.Commands.UpdateProfile;
using AgroLink.Application.Features.Auth.DTOs;
using AgroLink.Application.Features.Auth.Queries.GetUserProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetProfile()
    {
        var user = await mediator.Send(new GetUserProfileQuery());
        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(user);
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<UserDto>> UpdateProfile(UpdateProfileRequest request)
    {
        var result = await mediator.Send(new UpdateProfileCommand(request));
        return Ok(result);
    }
}
