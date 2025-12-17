namespace AgroLink.Application.Features.Paddocks.DTOs;

public class PaddockDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int FarmId { get; set; }
    public string FarmName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreatePaddockDto
{
    public string Name { get; set; } = string.Empty;
    public int FarmId { get; set; }
}

public class UpdatePaddockDto
{
    public string? Name { get; set; }
    public int? FarmId { get; set; }
}
