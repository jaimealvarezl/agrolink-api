namespace AgroLink.Application.Features.Lots.DTOs;

public class LotDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PaddockId { get; set; }
    public string PaddockName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int FarmId { get; set; }
}

public class CreateLotDto
{
    public string Name { get; set; } = string.Empty;
    public int PaddockId { get; set; }
    public string? Status { get; set; }
}

public class UpdateLotDto
{
    public string? Name { get; set; }
    public int? PaddockId { get; set; }
    public string? Status { get; set; }
}
