namespace AgroLink.Application.Features.Paddocks.DTOs;

public class PaddockDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required int FarmId { get; set; }
    public required string FarmName { get; set; }
    public decimal? Area { get; set; }
    public string? AreaType { get; set; }
    public required DateTime CreatedAt { get; set; }
}
