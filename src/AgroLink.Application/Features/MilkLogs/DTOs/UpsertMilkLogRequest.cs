namespace AgroLink.Application.Features.MilkLogs.DTOs;

public class UpsertMilkLogRequest
{
    public DateOnly Date { get; set; }
    public decimal TotalLiters { get; set; }
    public decimal? PricePerLiter { get; set; }
    public string? Notes { get; set; }
}
