using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Lots.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Lots.Commands.UpdatePaddock;

public class UpdateLotPaddockCommandHandler(
    ILotRepository lotRepository,
    IPaddockRepository paddockRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<UpdateLotPaddockCommand, LotDto>
{
    public async Task<LotDto> Handle(
        UpdateLotPaddockCommand request,
        CancellationToken cancellationToken
    )
    {
        var lot = await lotRepository.GetLotWithPaddockAsync(request.LotId);
        if (lot == null)
        {
            throw new NotFoundException("Lot", request.LotId);
        }

        if (lot.Paddock.FarmId != request.FarmId)
        {
            throw new ForbiddenAccessException("Lot does not belong to the provided farm");
        }

        var newPaddock = await paddockRepository.GetByIdAsync(request.NewPaddockId);
        if (newPaddock == null)
        {
            throw new NotFoundException("Paddock", request.NewPaddockId);
        }

        if (newPaddock.FarmId != request.FarmId)
        {
            throw new ForbiddenAccessException(
                "Destination paddock does not belong to the provided farm"
            );
        }

        lot.PaddockId = request.NewPaddockId;
        lot.UpdatedAt = DateTime.UtcNow;

        lotRepository.Update(lot);
        await unitOfWork.SaveChangesAsync();

        return new LotDto
        {
            Id = lot.Id,
            Name = lot.Name,
            PaddockId = lot.PaddockId,
            FarmId = newPaddock.FarmId,
            PaddockName = newPaddock.Name,
            Status = lot.Status,
            AnimalCount = lot.Animals?.Count ?? 0,
            CreatedAt = lot.CreatedAt,
        };
    }
}
