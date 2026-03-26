using AgroLink.Domain.Enums;

namespace AgroLink.Application.Features.Animals.DTOs;

public class AnimalRetirementDto
{
    public int Id { get; set; }
    public int AnimalId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public RetirementReason Reason { get; set; }
    public DateTime At { get; set; }
    public decimal? SalePrice { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
