using System.ComponentModel.DataAnnotations;

namespace AgroLink.Api.DTOs.Farms;

public class CreateFarmRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
}
