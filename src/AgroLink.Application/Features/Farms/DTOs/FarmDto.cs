namespace AgroLink.Application.Features.Farms.DTOs;

public class FarmDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public string? Location { get; set; }
    public required int OwnerId { get; set; }
    public required string Role { get; set; }
    public required DateTime CreatedAt { get; set; }
}
