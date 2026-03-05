using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Farms.Commands.Create;

public record CreateFarmCommand(string Name, string? Location, string? CUE, int UserId)
    : IRequest<FarmDto>;

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
        var userId = request.UserId;

        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new UnauthorizedAccessException($"User with ID {userId} from token not found.");
        }

        try
        {
            await unitOfWork.BeginTransactionAsync();

            // 1. Create the Owner first (with FarmId = null)
            var owner = new Owner { Name = user.Name, UserId = userId };

            await ownerRepository.AddAsync(owner);

            // Save to get the owner.Id
            await unitOfWork.SaveChangesAsync();

            // 2. Create the Farm (linked to the newly created Owner)
            var farm = new Farm
            {
                Name = request.Name,
                Location = request.Location,
                CUE = request.CUE,
                OwnerId = owner.Id, // Link via FK instead of navigation property initially
            };

            await farmRepository.AddAsync(farm);

            // Save to get the farm.Id
            await unitOfWork.SaveChangesAsync();

            // 3. Link the Owner back to the Farm
            owner.FarmId = farm.Id;

            // 4. Create the FarmMember record
            var member = new FarmMember
            {
                FarmId = farm.Id,
                UserId = userId,
                Role = FarmMemberRoles.Owner,
            };

            await farmMemberRepository.AddAsync(member);

            // Final save persists the owner.FarmId link and the member
            await unitOfWork.SaveChangesAsync();

            await unitOfWork.CommitTransactionAsync();

            return new FarmDto
            {
                Id = farm.Id,
                Name = farm.Name,
                Location = farm.Location,
                CUE = farm.CUE,
                OwnerId = farm.OwnerId,
                Role = member.Role,
                CreatedAt = farm.CreatedAt,
            };
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
