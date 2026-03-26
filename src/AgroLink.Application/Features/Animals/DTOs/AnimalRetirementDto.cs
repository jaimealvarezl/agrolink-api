using AgroLink.Domain.Entities;
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

    public static AnimalRetirementDto From(AnimalRetirement retirement) =>
        new()
        {
            Id = retirement.Id,
            AnimalId = retirement.AnimalId,
            UserId = retirement.UserId,
            UserName = retirement.User?.Name ?? string.Empty,
            Reason = retirement.Reason,
            At = retirement.At,
            SalePrice = retirement.SalePrice,
            Notes = retirement.Notes,
            CreatedAt = retirement.CreatedAt,
        };
}
