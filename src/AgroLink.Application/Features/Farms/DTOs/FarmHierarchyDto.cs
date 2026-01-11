namespace AgroLink.Application.Features.Farms.DTOs;

public class FarmHierarchyDto
{
    public int FarmId { get; set; }
    public string FarmName { get; set; } = string.Empty;
    public List<PaddockHierarchyDto> Paddocks { get; set; } = [];
}
