using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Farms.Commands.Update;

public record UpdateFarmCommand(int Id, string? Name, string? Location) : IRequest<FarmDto>;

public class UpdateFarmCommandHandler(IFarmRepository farmRepository, IUnitOfWork unitOfWork)
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

        if (!string.IsNullOrEmpty(request.Name))
        {
            farm.Name = request.Name;
        }

        if (request.Location != null)
        {
            farm.Location = request.Location;
        }

        farm.UpdatedAt = DateTime.UtcNow;

        farmRepository.Update(farm);
        await unitOfWork.SaveChangesAsync();

        return new FarmDto
        {
            Id = farm.Id,
            Name = farm.Name,
            Location = farm.Location,
            OwnerId = farm.OwnerId,
            Role = string.Empty,
            CreatedAt = farm.CreatedAt,
        };
    }
}
