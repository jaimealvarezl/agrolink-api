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

public class UpdateProfileRequest
{
    [Required]
    public required string Name { get; set; }
}
