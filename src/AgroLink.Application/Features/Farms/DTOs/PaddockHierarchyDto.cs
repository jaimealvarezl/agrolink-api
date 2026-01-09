namespace AgroLink.Application.Features.Farms.DTOs;

public class PaddockHierarchyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<LotHierarchyDto> Lots { get; set; } = [];
}
