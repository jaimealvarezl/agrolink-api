namespace AgroLink.Application.Features.Farms.DTOs;

public class PaddockHierarchyDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required List<LotHierarchyDto> Lots { get; set; }
}
