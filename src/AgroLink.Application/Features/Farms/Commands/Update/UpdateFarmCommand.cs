using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Farms.Commands.Update;

public record UpdateFarmCommand(int Id, string Name, string? Location, string? CUE, int UserId)
    : IRequest<FarmDto>;

public class UpdateFarmCommandHandler(
    IFarmRepository farmRepository,
    IFarmMemberRepository farmMemberRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<UpdateFarmCommand, FarmDto>
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

        var membership = await farmMemberRepository.FirstOrDefaultAsync(m =>
            m.FarmId == request.Id && m.UserId == request.UserId
        );

        if (
            membership == null
            || (
                membership.Role != FarmMemberRoles.Owner && membership.Role != FarmMemberRoles.Admin
            )
        )
        {
            throw new ArgumentException("Farm not found");
        }

        farm.Name = request.Name;

        if (request.Location != null)
        {
            farm.Location = request.Location;
        }

        if (request.CUE != null)
        {
            farm.CUE = request.CUE;
        }

        farm.UpdatedAt = DateTime.UtcNow;

        farmRepository.Update(farm);
        await unitOfWork.SaveChangesAsync();

        return new FarmDto
        {
            Id = farm.Id,
            Name = farm.Name,
            Location = farm.Location,
            CUE = farm.CUE,
            OwnerId = farm.OwnerId,
            Role = membership.Role,
            CreatedAt = farm.CreatedAt,
        };
    }
}
