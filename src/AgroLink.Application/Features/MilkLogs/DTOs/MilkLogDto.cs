namespace AgroLink.Application.Features.MilkLogs.DTOs;

public class MilkLogDto
{
    public int Id { get; set; }
    public int FarmId { get; set; }
    public DateOnly Date { get; set; }
    public decimal TotalLiters { get; set; }
    public decimal? PricePerLiter { get; set; }
    public decimal? RevenueTotal { get; set; }
    public int UserId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
