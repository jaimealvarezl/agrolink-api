using System.ComponentModel.DataAnnotations;

namespace AgroLink.Api.DTOs.Farms;

public class UpdateFarmRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Location { get; set; }

    [RegularExpression(@"^[a-zA-Z0-9]*$", ErrorMessage = "CUE must be alphanumeric.")]
    [MaxLength(20)]
    public string? CUE { get; set; }
}
