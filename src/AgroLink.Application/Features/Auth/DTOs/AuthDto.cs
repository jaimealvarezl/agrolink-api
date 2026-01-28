namespace AgroLink.Application.Features.Auth.DTOs;

public class UserDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }
    public required bool IsActive { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class LoginDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class AuthResponseDto
{
    public required string Token { get; set; }
    public required UserDto User { get; set; }
    public required DateTime ExpiresAt { get; set; }
}

public class RegisterRequest
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public string? Role { get; set; }
}

public class ValidateTokenRequest
{
    public required string Token { get; set; }
}

public class ValidateTokenResponse
{
    public required bool Valid { get; set; }
}
