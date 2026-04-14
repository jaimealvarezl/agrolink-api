using System.ComponentModel.DataAnnotations;

namespace AgroLink.Application.Features.Owners.DTOs;

public class OwnerDto
{
    public required int Id { get; set; }

    [Required]
    public required string Name { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public int? UserId { get; set; }
    public bool IsActive { get; set; } = true;

    public int AnimalCount { get; set; }

    [Required]
    public required DateTime CreatedAt { get; set; }
}
