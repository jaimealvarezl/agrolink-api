using System.ComponentModel.DataAnnotations;

namespace AgroLink.Api.DTOs.OwnerBrands;

public class CreateOwnerBrandRequest
{
    [Required]
    [MaxLength(100)]
    public string RegistrationNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? PhotoUrl { get; set; }
}
