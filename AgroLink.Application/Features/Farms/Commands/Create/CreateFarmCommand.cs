using AgroLink.Application.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Farms.Commands.Create;

public record CreateFarmCommand(CreateFarmDto Dto) : IRequest<FarmDto>;

public class CreateFarmCommandHandler(IFarmRepository farmRepository)
    : IRequestHandler<CreateFarmCommand, FarmDto>
{
    public async Task<FarmDto> Handle(
        CreateFarmCommand request,
        CancellationToken cancellationToken
    )
    {
        var dto = request.Dto;
        var farm = new Farm { Name = dto.Name, Location = dto.Location };

        await farmRepository.AddAsync(farm);

        return new FarmDto
        {
            Id = farm.Id,
            Name = farm.Name,
            Location = farm.Location,
            CreatedAt = farm.CreatedAt,
        };
    }
}
