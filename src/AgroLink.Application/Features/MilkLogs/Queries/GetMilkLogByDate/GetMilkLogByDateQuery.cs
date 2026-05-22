using AgroLink.Application.Features.MilkLogs.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.MilkLogs.Queries.GetMilkLogByDate;

public record GetMilkLogByDateQuery(int FarmId, DateOnly Date) : IRequest<MilkLogDto?>;

public class GetMilkLogByDateQueryHandler(IDailyMilkLogRepository milkLogRepository)
    : IRequestHandler<GetMilkLogByDateQuery, MilkLogDto?>
{
    public async Task<MilkLogDto?> Handle(
        GetMilkLogByDateQuery request,
        CancellationToken cancellationToken
    )
    {
        var log = await milkLogRepository.FindByDateAsync(
            request.FarmId,
            request.Date,
            cancellationToken
        );

        return log?.ToDto();
    }
}
