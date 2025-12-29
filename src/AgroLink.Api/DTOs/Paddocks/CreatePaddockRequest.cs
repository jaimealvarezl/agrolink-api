using System.ComponentModel.DataAnnotations;

namespace AgroLink.Api.DTOs.Paddocks;

public class CreatePaddockRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int FarmId { get; set; }

    public decimal? Area { get; set; }

    public string? AreaType { get; set; }
}
