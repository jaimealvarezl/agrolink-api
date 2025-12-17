using AgroLink.Application.Features.Paddocks.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Paddocks.Commands.Update;

public record UpdatePaddockCommand(int Id, UpdatePaddockDto Dto) : IRequest<PaddockDto>;

public class UpdatePaddockCommandHandler(
    IPaddockRepository paddockRepository,
    IFarmRepository farmRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<UpdatePaddockCommand, PaddockDto>
{
    public async Task<PaddockDto> Handle(
        UpdatePaddockCommand request,
        CancellationToken cancellationToken
    )
    {
        var paddock = await paddockRepository.GetByIdAsync(request.Id);
        if (paddock == null)
        {
            throw new ArgumentException("Paddock not found");
        }

        var dto = request.Dto;
        if (!string.IsNullOrEmpty(dto.Name))
        {
            paddock.Name = dto.Name;
        }

        if (dto.FarmId.HasValue)
        {
            paddock.FarmId = dto.FarmId.Value;
        }

        paddock.UpdatedAt = DateTime.UtcNow;

        paddockRepository.Update(paddock);
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
