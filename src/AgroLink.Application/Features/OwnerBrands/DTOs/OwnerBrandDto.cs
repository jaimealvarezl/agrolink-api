namespace AgroLink.Application.Features.OwnerBrands.DTOs;

public class OwnerBrandDto
{
    public required int Id { get; set; }
    public required int OwnerId { get; set; }
    public required string RegistrationNumber { get; set; }
    public required string Description { get; set; }
    public string? PhotoUrl { get; set; }
    public required bool IsActive { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
