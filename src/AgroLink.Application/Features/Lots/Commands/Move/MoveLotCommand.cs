using AgroLink.Application.Features.Lots.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Lots.Commands.Move;

public record MoveLotCommand(int LotId, int ToPaddockId, string? Reason, int UserId)
    : IRequest<LotDto>;

public class MoveLotCommandHandler(
    ILotRepository lotRepository,
    IPaddockRepository paddockRepository,
    IMovementRepository movementRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<MoveLotCommand, LotDto>
{
    public async Task<LotDto> Handle(MoveLotCommand request, CancellationToken cancellationToken)
    {
        var lot = await lotRepository.GetByIdAsync(request.LotId);
        if (lot == null)
        {
            throw new ArgumentException("Lot not found");
        }

        var fromPaddockId = lot.PaddockId;
        lot.PaddockId = request.ToPaddockId;
        lot.UpdatedAt = DateTime.UtcNow;

        lotRepository.Update(lot);

        // Record movement
        var movement = new Movement
        {
            EntityType = "LOT",
            EntityId = request.LotId,
            FromId = fromPaddockId,
            ToId = request.ToPaddockId,
            At = DateTime.UtcNow,
            Reason = request.Reason,
            UserId = request.UserId,
        };

        await movementRepository.AddMovementAsync(movement);
        await unitOfWork.SaveChangesAsync();

        var paddock = await paddockRepository.GetByIdAsync(lot.PaddockId);

        return new LotDto
        {
            Id = lot.Id,
            Name = lot.Name,
            PaddockId = lot.PaddockId,
            PaddockName = paddock?.Name ?? "",
            Status = lot.Status,
            CreatedAt = lot.CreatedAt,
        };
    }
}
