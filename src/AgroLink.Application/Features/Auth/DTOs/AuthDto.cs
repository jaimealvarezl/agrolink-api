using System.ComponentModel.DataAnnotations;

namespace AgroLink.Application.Features.Auth.DTOs;

public class UserDto
{
    public required int Id { get; set; }

    [Required]
    public required string Name { get; set; }

    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string Role { get; set; }

    [Required]
    public required bool IsActive { get; set; }

    [Required]
    public required DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class LoginDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string Password { get; set; }
}

public class AuthResponseDto
{
    [Required]
    public required string Token { get; set; }

    [Required]
    public required UserDto User { get; set; }

    [Required]
    public required DateTime ExpiresAt { get; set; }
}

public class RegisterRequest
{
    [Required]
    public required string Name { get; set; }

    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [MinLength(6)]
    public required string Password { get; set; }
    public string? Role { get; set; }
}

public class ValidateTokenRequest
{
    [Required]
    public required string Token { get; set; }
}

public class ValidateTokenResponse
{
    [Required]
    public required bool Valid { get; set; }
}
