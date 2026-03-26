using System.ComponentModel.DataAnnotations;
using AgroLink.Domain.Enums;

namespace AgroLink.Application.Features.Animals.DTOs;

public class RetireAnimalRequest
{
    [Required]
    public RetirementReason Reason { get; set; }

    [Required]
    public DateTime At { get; set; }

    public decimal? SalePrice { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
