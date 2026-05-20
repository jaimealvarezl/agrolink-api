using AgroLink.Application.Features.MilkLogs.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.MilkLogs.Queries.GetMilkLogs;

public record GetMilkLogsQuery(
    int FarmId,
    DateOnly? From,
    DateOnly? To,
    int Page = 1,
    int PageSize = 30
) : IRequest<MilkLogsListDto>;

public class GetMilkLogsQueryHandler(
    IDailyMilkLogRepository milkLogRepository,
    IDateTimeProvider dateTimeProvider
) : IRequestHandler<GetMilkLogsQuery, MilkLogsListDto>
{
    private const int DefaultRangeDays = 30;

    public async Task<MilkLogsListDto> Handle(
        GetMilkLogsQuery request,
        CancellationToken cancellationToken
    )
    {
        if (request.Page < 1)
        {
            throw new ArgumentException("Page must be greater than or equal to 1.");
        }

        if (request.PageSize < 1)
        {
            throw new ArgumentException("PageSize must be greater than or equal to 1.");
        }

        var today = dateTimeProvider.TodayUtc;
        var to = request.To ?? today;
        var from = request.From ?? today.AddDays(-DefaultRangeDays);

        var (items, totalCount) = await milkLogRepository.GetPagedByDateRangeAsync(
            request.FarmId,
            from,
            to,
            request.Page,
            request.PageSize,
            cancellationToken
        );

        var lastPrice = await milkLogRepository.FindLastPricePerLiterAsync(
            request.FarmId,
            cancellationToken
        );

        return new MilkLogsListDto
        {
            Items = items.Select(log => log.ToDto()),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            LastUsedPricePerLiter = lastPrice,
        };
    }
}
