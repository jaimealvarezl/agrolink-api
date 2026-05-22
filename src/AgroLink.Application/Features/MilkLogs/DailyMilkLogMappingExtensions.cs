using AgroLink.Application.Features.MilkLogs.DTOs;
using AgroLink.Domain.Entities;

namespace AgroLink.Application.Features.MilkLogs;

internal static class DailyMilkLogMappingExtensions
{
    internal static MilkLogDto ToDto(this DailyMilkLog log)
    {
        return new MilkLogDto
        {
            Id = log.Id,
            FarmId = log.FarmId,
            Date = log.Date,
            TotalLiters = log.TotalLiters,
            PricePerLiter = log.PricePerLiter,
            RevenueTotal = log.PricePerLiter.HasValue
                ? Math.Round(
                    log.TotalLiters * log.PricePerLiter.Value,
                    2,
                    MidpointRounding.AwayFromZero
                )
                : null,
            UserId = log.UserId,
            Notes = log.Notes,
            CreatedAt = log.CreatedAt,
            UpdatedAt = log.UpdatedAt,
        };
    }
}
