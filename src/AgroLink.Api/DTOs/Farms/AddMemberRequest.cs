using System.ComponentModel.DataAnnotations;

namespace AgroLink.Api.DTOs.Farms;

public record AddMemberRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Role { get; init; } = string.Empty;
}
