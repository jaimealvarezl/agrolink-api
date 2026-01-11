namespace AgroLink.Application.Features.Farms.DTOs;

public class FarmHierarchyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<PaddockHierarchyDto> Paddocks { get; set; } = new();
}
