using System.ComponentModel.DataAnnotations;

namespace AgroLink.Application.Features.Paddocks.DTOs;

public class PaddockDto
{
    public required int Id { get; set; }

    [Required]
    public required string Name { get; set; }

    [Required]
    public required int FarmId { get; set; }

    [Required]
    public required string FarmName { get; set; }
    public decimal? Area { get; set; }
    public string? AreaType { get; set; }

    [Required]
    public required DateTime CreatedAt { get; set; }
}
