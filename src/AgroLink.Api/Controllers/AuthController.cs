using AgroLink.Application.Features.Auth.Commands.Login;
using AgroLink.Application.Features.Auth.Commands.Register;
using AgroLink.Application.Features.Auth.DTOs;
using AgroLink.Application.Features.Auth.Queries.GetUserProfile;
using AgroLink.Application.Features.Auth.Queries.ValidateToken;
using AgroLink.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// Added this using directive
// Added this using directive

namespace AgroLink.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ITokenExtractionService tokenExtractionService, IMediator mediator)
    : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var result = await mediator.Send(new LoginCommand(dto));
        if (result == null)
        {
            return Unauthorized("Invalid credentials");
        }

        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequest request)
    {
        try
        {
            var result = await mediator.Send(new RegisterCommand(request));
            return CreatedAtAction(nameof(GetProfile), new { id = result.User.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetProfile()
    {
        var token = ExtractTokenFromRequest();
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized();
        }

        var user = await mediator.Send(new GetUserProfileQuery(token));
        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(user);
    }

    [HttpPost("validate")]
    public async Task<ActionResult<ValidateTokenResponse>> ValidateToken(
        [FromBody] ValidateTokenRequest request
    )
    {
        var result = await mediator.Send(new ValidateTokenQuery(request.Token));
        return Ok(result);
    }

    private string? ExtractTokenFromRequest()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        return tokenExtractionService.ExtractTokenFromHeader(authHeader ?? string.Empty);
    }
}
