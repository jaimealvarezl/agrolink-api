using AgroLink.Core.DTOs;
using AgroLink.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var result = await authService.LoginAsync(dto);
        if (result == null)
            return Unauthorized("Invalid credentials");

        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterRequest request)
    {
        try
        {
            var userDto = new UserDto { Name = request.Name, Email = request.Email, Role = request.Role ?? "USER", };

            var user = await authService.RegisterAsync(userDto, request.Password);
            return CreatedAtAction(nameof(GetProfile), new { id = user.Id }, user);
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
        var token = GetTokenFromHeader();
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        var user = await authService.GetUserFromTokenAsync(token);
        if (user == null)
            return Unauthorized();

        return Ok(user);
    }

    [HttpPost("validate")]
    public async Task<ActionResult<object>> ValidateToken([FromBody] ValidateTokenRequest request)
    {
        var isValid = await authService.ValidateTokenAsync(request.Token);
        return Ok(new { valid = isValid });
    }

    private string? GetTokenFromHeader()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ") == true)
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        return null;
    }
}

public class RegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Role { get; set; }
}

public class ValidateTokenRequest
{
    public string Token { get; set; } = string.Empty;
}
