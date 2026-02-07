using System.ComponentModel.DataAnnotations;

namespace AgroLink.Application.Features.Farms.DTOs;

public class FarmHierarchyDto
{
    public required int Id { get; set; }

    [Required]
    public required string Name { get; set; }

    [Required]
    public required List<PaddockHierarchyDto> Paddocks { get; set; }
}
