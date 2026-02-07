using System.ComponentModel.DataAnnotations;

namespace AgroLink.Application.Features.Farms.DTOs;

public class PaddockHierarchyDto
{
    public required int Id { get; set; }

    [Required]
    public required string Name { get; set; }

    [Required]
    public required List<LotHierarchyDto> Lots { get; set; }
}
