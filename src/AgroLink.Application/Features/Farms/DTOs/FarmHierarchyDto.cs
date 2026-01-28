namespace AgroLink.Application.Features.Farms.DTOs;

public class FarmHierarchyDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required List<PaddockHierarchyDto> Paddocks { get; set; }
}
