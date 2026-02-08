using System.ComponentModel.DataAnnotations;

namespace AgroLink.Application.Features.Owners.DTOs;

public class OwnerDto
{
    public required int Id { get; set; }

    [Required]
    public required string Name { get; set; }

    public string? Phone { get; set; }

    [Required]
    public required DateTime CreatedAt { get; set; }
}
