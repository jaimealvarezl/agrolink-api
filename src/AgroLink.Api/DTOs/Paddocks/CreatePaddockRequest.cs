using System.ComponentModel.DataAnnotations;

namespace AgroLink.Api.DTOs.Paddocks;

public class CreatePaddockRequest
{
    [Required]
    public required string Name { get; set; }

    [Required]
    public required int FarmId { get; set; }

    public decimal? Area { get; set; }

    public string? AreaType { get; set; }
}
