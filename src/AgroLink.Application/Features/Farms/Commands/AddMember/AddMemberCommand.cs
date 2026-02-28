using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Farms.Commands.AddMember;

public record AddMemberCommand(int FarmId, string Email, string Role) : IRequest<FarmMemberDto>;

public class AddMemberCommandHandler(
    IFarmMemberRepository farmMemberRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<AddMemberCommand, FarmMemberDto>
{
    public async Task<FarmMemberDto> Handle(
        AddMemberCommand request,
        CancellationToken cancellationToken
    )
    {
        var user = await userRepository.GetByEmailAsync(request.Email);
        if (user == null)
        {
            throw new ArgumentException("User not found. They must register in AgroLink first.");
        }

        var existingMember = await farmMemberRepository.GetByFarmAndUserAsync(
            request.FarmId,
            user.Id
        );
        if (existingMember != null)
        {
            throw new ArgumentException("User is already a member of this farm.");
        }

        var member = new FarmMember
        {
            FarmId = request.FarmId,
            UserId = user.Id,
            Role = request.Role,
            JoinedAt = DateTime.UtcNow,
        };

        await farmMemberRepository.AddAsync(member);
        await unitOfWork.SaveChangesAsync();

        return new FarmMemberDto
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = member.Role,
            JoinedAt = member.JoinedAt,
        };
    }
}
