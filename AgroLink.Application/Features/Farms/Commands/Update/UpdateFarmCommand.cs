using AgroLink.Application.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Farms.Commands.Update;

public record UpdateFarmCommand(int Id, UpdateFarmDto Dto) : IRequest<FarmDto>;

public class UpdateFarmCommandHandler(IFarmRepository farmRepository)
    : IRequestHandler<UpdateFarmCommand, FarmDto>
{
    public async Task<FarmDto> Handle(
        UpdateFarmCommand request,
        CancellationToken cancellationToken
    )
    {
        var farm = await farmRepository.GetByIdAsync(request.Id);
        if (farm == null)
        {
            throw new ArgumentException("Farm not found");
        }

        var dto = request.Dto;
        if (!string.IsNullOrEmpty(dto.Name))
        {
            farm.Name = dto.Name;
        }

        if (dto.Location != null)
        {
            farm.Location = dto.Location;
        }

        farm.UpdatedAt = DateTime.UtcNow;

        farmRepository.Update(farm);
        await farmRepository.SaveChangesAsync();

        return new FarmDto
        {
            Id = farm.Id,
            Name = farm.Name,
            Location = farm.Location,
            CreatedAt = farm.CreatedAt,
        };
    }
}
