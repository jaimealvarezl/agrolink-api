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

            var farm = new Farm
            {
                Name = request.Name,
                Location = request.Location,
                CUE = request.CUE,
            };

            var owner = new Owner
            {
                Name = user.Name,
                UserId = userId,
                Farm = farm, // Link them immediately to satisfy EF Core mapping
            };

            await ownerRepository.AddAsync(owner);

            farm.Owner = owner;

            await farmRepository.AddAsync(farm);

            // First save so Farm gets an ID and Owner gets an ID.
            await unitOfWork.SaveChangesAsync();

            // Link the FarmId back to the owner
            owner.FarmId = farm.Id;

            var member = new FarmMember
            {
                Farm = farm,
                UserId = userId,
                Role = FarmMemberRoles.Owner,
            };

            await farmMemberRepository.AddAsync(member);

            // Second save persists the owner.FarmId link and the member
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
