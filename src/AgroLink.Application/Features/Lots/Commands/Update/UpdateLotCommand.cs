using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Lots.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Lots.Commands.Update;

public record UpdateLotCommand(int Id, UpdateLotDto Dto) : IRequest<LotDto>;

public class UpdateLotCommandHandler(
    ILotRepository lotRepository,
    IPaddockRepository paddockRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService
) : IRequestHandler<UpdateLotCommand, LotDto>
{
    public async Task<LotDto> Handle(UpdateLotCommand request, CancellationToken cancellationToken)
    {
        var lot = await lotRepository.GetByIdAsync(request.Id);
        if (lot == null)
        {
            throw new ArgumentException("Lot not found");
        }

        // Security check: ensure lot belongs to the current farm context
        if (currentUserService.CurrentFarmId.HasValue)
        {
            var paddockContext = await paddockRepository.GetByIdAsync(lot.PaddockId);
            if (
                paddockContext != null
                && paddockContext.FarmId != currentUserService.CurrentFarmId.Value
            )
            {
                throw new ForbiddenAccessException("You do not have access to this lot");
            }
        }

        var dto = request.Dto;
        if (!string.IsNullOrEmpty(dto.Name))
        {
            lot.Name = dto.Name;
        }

        if (dto.PaddockId.HasValue)
        {
            lot.PaddockId = dto.PaddockId.Value;
        }

        if (!string.IsNullOrEmpty(dto.Status))
        {
            lot.Status = dto.Status;
        }

        lot.UpdatedAt = DateTime.UtcNow;

        lotRepository.Update(lot);
        await unitOfWork.SaveChangesAsync();

        var paddock = await paddockRepository.GetByIdAsync(lot.PaddockId);

        return new LotDto
        {
            Id = lot.Id,
            Name = lot.Name,
            PaddockId = lot.PaddockId,
            FarmId = paddock?.FarmId ?? 0,
            PaddockName = paddock?.Name ?? "",
            Status = lot.Status,
            AnimalCount = lot.Animals?.Count ?? 0,
            CreatedAt = lot.CreatedAt,
        };
    }
}
