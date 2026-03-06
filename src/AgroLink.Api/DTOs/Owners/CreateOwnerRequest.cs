using System.ComponentModel.DataAnnotations;

namespace AgroLink.Api.DTOs.Owners;

public class CreateOwnerRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(255)]
    [EmailAddress]
    public string? Email { get; set; }

    public int? UserId { get; set; }
}
