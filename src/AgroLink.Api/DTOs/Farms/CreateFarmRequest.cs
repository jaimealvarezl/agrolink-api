using System.ComponentModel.DataAnnotations;

namespace AgroLink.Api.DTOs.Farms;

public class CreateFarmRequest
{
    [Required]
    public required string Name { get; set; }

    public string? Location { get; set; }

    public string? CUE { get; set; }
}
