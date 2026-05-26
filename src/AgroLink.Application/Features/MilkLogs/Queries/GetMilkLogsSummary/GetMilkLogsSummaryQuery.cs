using AgroLink.Application.Features.MilkLogs.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.MilkLogs.Queries.GetMilkLogsSummary;

public record GetMilkLogsSummaryQuery(int FarmId, DateOnly? From, DateOnly? To)
    : IRequest<MilkLogsSummaryDto>;

public class GetMilkLogsSummaryQueryHandler(
    IDailyMilkLogRepository milkLogRepository,
    IDateTimeProvider dateTimeProvider
) : IRequestHandler<GetMilkLogsSummaryQuery, MilkLogsSummaryDto>
{
    private const int DefaultRangeDays = 30;

    public async Task<MilkLogsSummaryDto> Handle(
        GetMilkLogsSummaryQuery request,
        CancellationToken cancellationToken
    )
    {
        var today = dateTimeProvider.TodayUtc;
        var to = request.To ?? today;
        var from = request.From ?? today.AddDays(-DefaultRangeDays);

        var rangeDays = to.DayNumber - from.DayNumber + 1;
        var pageSize = Math.Max(rangeDays, 1);
        var (items, totalCount) = await milkLogRepository.GetPagedByDateRangeAsync(
            request.FarmId,
            from,
            to,
            1,
            pageSize,
            cancellationToken
        );

        var logs = items.ToList();
        var totalLiters = logs.Sum(l => l.TotalLiters);
        var totalRevenue = logs.Where(l => l.PricePerLiter.HasValue)
            .Sum(l => l.TotalLiters * l.PricePerLiter!.Value);
        var pricedLogs = logs.Where(l => l.PricePerLiter.HasValue).ToList();
        decimal? avgPricePerLiter =
            pricedLogs.Count > 0 ? pricedLogs.Average(l => l.PricePerLiter!.Value) : null;

        return new MilkLogsSummaryDto(
            totalLiters,
            totalRevenue,
            avgPricePerLiter,
            totalCount,
            from,
            to
        );
    }
}
