namespace AgroLink.Application.Features.Farms.DTOs;

public class FarmDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int OwnerId { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateFarmDto
{
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
}

public class UpdateFarmDto
{
    public string? Name { get; set; }
    public string? Location { get; set; }
}
