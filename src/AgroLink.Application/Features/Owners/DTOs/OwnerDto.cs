namespace AgroLink.Application.Features.Owners.DTOs;

public class OwnerDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public string? Phone { get; set; }
    public required DateTime CreatedAt { get; set; }
}
