using System.ComponentModel.DataAnnotations;
using AgroLink.Domain.Enums;

namespace AgroLink.Application.Features.Animals.DTOs;

public class RetireAnimalRequest
{
    [Required]
    public RetirementReason Reason { get; set; }

    [Required]
    public DateTime At { get; set; }

    [Range(0, (double)decimal.MaxValue, ErrorMessage = "SalePrice must be a non-negative value.")]
    public decimal? SalePrice { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
