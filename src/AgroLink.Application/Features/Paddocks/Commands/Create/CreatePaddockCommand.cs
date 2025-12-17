using AgroLink.Application.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Paddocks.Commands.Create;

public record CreatePaddockCommand(CreatePaddockDto Dto) : IRequest<PaddockDto>;

public class CreatePaddockCommandHandler(
    IPaddockRepository paddockRepository,
    IFarmRepository farmRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreatePaddockCommand, PaddockDto>
{
    public async Task<PaddockDto> Handle(
        CreatePaddockCommand request,
        CancellationToken cancellationToken
    )
    {
        var dto = request.Dto;
        var paddock = new Paddock { Name = dto.Name, FarmId = dto.FarmId };

        await paddockRepository.AddAsync(paddock);
        await unitOfWork.SaveChangesAsync();

        var farm = await farmRepository.GetByIdAsync(paddock.FarmId);

        return new PaddockDto
        {
            Id = paddock.Id,
            Name = paddock.Name,
            FarmId = paddock.FarmId,
            FarmName = farm?.Name ?? "",
            CreatedAt = paddock.CreatedAt,
        };
    }
}
