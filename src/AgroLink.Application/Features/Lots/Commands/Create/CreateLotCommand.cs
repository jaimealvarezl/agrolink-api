using AgroLink.Application.Features.Lots.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Lots.Commands.Create;

public record CreateLotCommand(CreateLotDto Dto) : IRequest<LotDto>;

public class CreateLotCommandHandler(
    ILotRepository lotRepository,
    IPaddockRepository paddockRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateLotCommand, LotDto>
{
    public async Task<LotDto> Handle(CreateLotCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var paddock = await paddockRepository.GetByIdAsync(dto.PaddockId);
        if (paddock == null)
        {
            throw new ArgumentException($"Paddock with ID {dto.PaddockId} not found.");
        }

        var lot = new Lot
        {
            Name = dto.Name,
            PaddockId = dto.PaddockId,
            Status = dto.Status ?? "ACTIVE",
        };

        await lotRepository.AddAsync(lot);
        await unitOfWork.SaveChangesAsync();

        return new LotDto
        {
            Id = lot.Id,
            Name = lot.Name,
            PaddockId = lot.PaddockId,
            PaddockName = paddock.Name,
            Status = lot.Status,
            CreatedAt = lot.CreatedAt,
            FarmId = lot.Paddock.FarmId,
        };
    }
}
