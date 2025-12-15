using AgroLink.Application.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IAuthService authService,
    ITokenExtractionService tokenExtractionService
) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var result = await authService.LoginAsync(dto);
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
            var result = await authService.RegisterUserAsync(request);
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

        var user = await authService.GetUserProfileAsync(token);
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
        var result = await authService.ValidateTokenResponseAsync(request.Token);
        return Ok(result);
    }

    private string? ExtractTokenFromRequest()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        return tokenExtractionService.ExtractTokenFromHeader(authHeader ?? string.Empty);
    }
}
