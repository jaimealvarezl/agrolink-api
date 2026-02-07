using System.ComponentModel.DataAnnotations;

namespace AgroLink.Application.Features.Farms.DTOs;

public class LotHierarchyDto
{
    public required int Id { get; set; }

    [Required]
    public required string Name { get; set; }

    [Required]
    public required int AnimalCount { get; set; }
}
