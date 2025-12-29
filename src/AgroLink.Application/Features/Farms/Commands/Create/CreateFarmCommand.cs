using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Farms.Commands.Create;

public record CreateFarmCommand(CreateFarmDto Dto, int UserId) : IRequest<FarmDto>;

public class CreateFarmCommandHandler(
    IFarmRepository farmRepository,
    IOwnerRepository ownerRepository,
    IFarmMemberRepository farmMemberRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateFarmCommand, FarmDto>
{
    public async Task<FarmDto> Handle(
        CreateFarmCommand request,
        CancellationToken cancellationToken
    )
    {
        var dto = request.Dto;
        var userId = request.UserId;

        // 1. Get User details
        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException($"User with ID {userId} from token not found.");

        // 2. Create Owner record (Legal Entity) - assuming name matches user for now if creating fresh
        var owner = new Owner
        {
            Name = user.Name,
            // Phone could be copied if available
        };

        await ownerRepository.AddAsync(owner);

        // 3. Create Farm
        var farm = new Farm
        {
            Name = dto.Name,
            Location = dto.Location,
            Owner = owner,
        };

        await farmRepository.AddAsync(farm);

        // 4. Create FarmMember
        var member = new FarmMember
        {
            Farm = farm,
            UserId = userId,
            Role = FarmMemberRoles.Owner,
        };

        await farmMemberRepository.AddAsync(member);

        await unitOfWork.SaveChangesAsync();

        return new FarmDto
        {
            Id = farm.Id,
            Name = farm.Name,
            Location = farm.Location,
            OwnerId = farm.OwnerId,
            Role = member.Role,
            CreatedAt = farm.CreatedAt,
        };
    }
}
