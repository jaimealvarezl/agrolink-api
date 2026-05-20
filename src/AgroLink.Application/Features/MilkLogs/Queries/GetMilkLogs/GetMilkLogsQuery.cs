using AgroLink.Application.Features.MilkLogs.Commands.UpsertMilkLog;
using AgroLink.Application.Features.MilkLogs.DTOs;
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

public class GetMilkLogsQueryHandler(IDailyMilkLogRepository milkLogRepository)
    : IRequestHandler<GetMilkLogsQuery, MilkLogsListDto>
{
    private const int DefaultRangeDays = 30;

    public async Task<MilkLogsListDto> Handle(
        GetMilkLogsQuery request,
        CancellationToken cancellationToken
    )
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
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
            Items = items.Select(UpsertMilkLogCommandHandler.MapToDto),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            LastUsedPricePerLiter = lastPrice,
        };
    }
}
