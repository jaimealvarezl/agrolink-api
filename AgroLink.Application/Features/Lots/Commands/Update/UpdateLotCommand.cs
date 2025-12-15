using AgroLink.Application.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Lots.Commands.Update;

public record UpdateLotCommand(int Id, UpdateLotDto Dto) : IRequest<LotDto>;

public class UpdateLotCommandHandler(
    ILotRepository lotRepository,
    IPaddockRepository paddockRepository
) : IRequestHandler<UpdateLotCommand, LotDto>
{
    public async Task<LotDto> Handle(UpdateLotCommand request, CancellationToken cancellationToken)
    {
        var lot = await lotRepository.GetByIdAsync(request.Id);
        if (lot == null)
        {
            throw new ArgumentException("Lot not found");
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
