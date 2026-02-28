using System.ComponentModel.DataAnnotations;

namespace AgroLink.Api.DTOs.Farms;

public record UpdateMemberRoleRequest
{
    [Required]
    public string Role { get; init; } = string.Empty;
}
