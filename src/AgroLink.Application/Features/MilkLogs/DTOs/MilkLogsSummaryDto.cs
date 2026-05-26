namespace AgroLink.Application.Features.MilkLogs.DTOs;

public record MilkLogsSummaryDto(
    decimal TotalLiters,
    decimal TotalRevenue,
    decimal? AvgPricePerLiter,
    int DaysLogged,
    DateOnly From,
    DateOnly To
);
