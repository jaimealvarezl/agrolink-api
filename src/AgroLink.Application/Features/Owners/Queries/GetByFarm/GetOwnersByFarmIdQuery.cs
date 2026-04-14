using AgroLink.Application.Features.Owners.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Owners.Queries.GetByFarm;

public record GetOwnersByFarmIdQuery(int FarmId) : IRequest<IEnumerable<OwnerDto>>;

public class GetOwnersByFarmIdQueryHandler(IOwnerRepository ownerRepository)
    : IRequestHandler<GetOwnersByFarmIdQuery, IEnumerable<OwnerDto>>
{
    public async Task<IEnumerable<OwnerDto>> Handle(
        GetOwnersByFarmIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var owners = await ownerRepository.GetOwnersByFarmAsync(request.FarmId);

        return owners.Select(o => new OwnerDto
        {
            Id = o.Id,
            Name = o.Name,
            Phone = o.Phone,
            Email = o.Email,
            UserId = o.UserId,
            IsActive = o.IsActive,
            AnimalCount = o.AnimalOwners.Count,
            CreatedAt = o.CreatedAt,
        });
    }
}
