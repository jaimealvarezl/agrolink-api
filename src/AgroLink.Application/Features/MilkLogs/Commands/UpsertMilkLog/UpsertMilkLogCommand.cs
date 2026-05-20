using AgroLink.Application.Features.MilkLogs.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.MilkLogs.Commands.UpsertMilkLog;

public record UpsertMilkLogCommand(int FarmId, int UserId, UpsertMilkLogRequest Dto)
    : IRequest<UpsertMilkLogResult>;

public record UpsertMilkLogResult(bool IsNew, MilkLogDto Log);

public class UpsertMilkLogCommandHandler(
    IDailyMilkLogRepository milkLogRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider
) : IRequestHandler<UpsertMilkLogCommand, UpsertMilkLogResult>
{
    public const int MaxDaysInPast = 30;
    public const decimal MaxLiters = 99999.99m;
    public const decimal MaxPricePerLiter = 9999.9999m;

    public async Task<UpsertMilkLogResult> Handle(
        UpsertMilkLogCommand request,
        CancellationToken cancellationToken
    )
    {
        var today = dateTimeProvider.TodayUtc;
        var dto = request.Dto;

        if (dto.Date > today)
        {
            throw new ArgumentException("Date cannot be in the future.");
        }

        if (dto.Date < today.AddDays(-MaxDaysInPast))
        {
            throw new ArgumentException(
                $"Date cannot be more than {MaxDaysInPast} days in the past."
            );
        }

        if (dto.TotalLiters < 0 || dto.TotalLiters > MaxLiters)
        {
            throw new ArgumentException($"TotalLiters must be between 0 and {MaxLiters}.");
        }

        if (
            dto.PricePerLiter.HasValue
            && (dto.PricePerLiter < 0 || dto.PricePerLiter > MaxPricePerLiter)
        )
        {
            throw new ArgumentException($"PricePerLiter must be between 0 and {MaxPricePerLiter}.");
        }

        var existing = await milkLogRepository.FindByDateAsync(
            request.FarmId,
            dto.Date,
            cancellationToken
        );
        var isNew = existing == null;

        if (isNew)
        {
            existing = new DailyMilkLog
            {
                FarmId = request.FarmId,
                Date = dto.Date,
                TotalLiters = dto.TotalLiters,
                PricePerLiter = dto.PricePerLiter,
                UserId = request.UserId,
                Notes = dto.Notes,
            };
            await milkLogRepository.AddAsync(existing, cancellationToken);
        }
        else
        {
            existing!.TotalLiters = dto.TotalLiters;
            existing.PricePerLiter = dto.PricePerLiter;
            existing.Notes = dto.Notes;
            existing.UserId = request.UserId;
            existing.UpdatedAt = dateTimeProvider.UtcNow;
            milkLogRepository.Update(existing);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpsertMilkLogResult(isNew, existing.ToDto());
    }
}
