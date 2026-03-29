using System.ComponentModel.DataAnnotations;

namespace AgroLink.Api.DTOs.OwnerBrands;

public class CreateOwnerBrandRequest
{
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}
