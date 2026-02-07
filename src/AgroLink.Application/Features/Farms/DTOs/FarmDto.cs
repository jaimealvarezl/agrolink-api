using System.ComponentModel.DataAnnotations;

namespace AgroLink.Application.Features.Farms.DTOs;

public class FarmDto
{
    public required int Id { get; set; }

    [Required]
    public required string Name { get; set; }
    public string? Location { get; set; }
    public string? CUE { get; set; }

    [Required]
    public required int OwnerId { get; set; }

    [Required]
    public required string Role { get; set; }

    [Required]
    public required DateTime CreatedAt { get; set; }
}
