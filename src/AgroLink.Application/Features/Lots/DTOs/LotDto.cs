namespace AgroLink.Application.Features.Lots.DTOs;

public class LotDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required int PaddockId { get; set; }
    public required int FarmId { get; set; }
    public required string PaddockName { get; set; }
    public required string Status { get; set; }
    public required int AnimalCount { get; set; }
    public required DateTime CreatedAt { get; set; }
}

public class CreateLotDto
{
    public required string Name { get; set; }
    public required int PaddockId { get; set; }
    public string? Status { get; set; }
}

public class UpdateLotDto
{
    public string? Name { get; set; }
    public int? PaddockId { get; set; }
    public string? Status { get; set; }
}
