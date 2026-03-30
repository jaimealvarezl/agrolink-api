using System.ComponentModel.DataAnnotations;

namespace AgroLink.Api.DTOs.AnimalBrands;

public class AddAnimalBrandRequest
{
    public required int OwnerBrandId { get; set; }
    public DateTime? AppliedAt { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
