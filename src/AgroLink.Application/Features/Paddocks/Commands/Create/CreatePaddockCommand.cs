using AgroLink.Application.Features.Paddocks.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Paddocks.Commands.Create;

public record CreatePaddockCommand(string Name, int FarmId, int UserId) : IRequest<PaddockDto>;

public class CreatePaddockCommandHandler(
    IPaddockRepository paddockRepository,
    IFarmRepository farmRepository,
    IFarmMemberRepository farmMemberRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreatePaddockCommand, PaddockDto>
{
    public async Task<PaddockDto> Handle(
        CreatePaddockCommand request,
        CancellationToken cancellationToken
    )
    {
        var userId = request.UserId;

        var farm = await farmRepository.GetByIdAsync(request.FarmId);
        if (farm == null)
            throw new ArgumentException($"Farm with ID {request.FarmId} not found.");

        var member = await farmMemberRepository.FirstOrDefaultAsync(m =>
            m.FarmId == request.FarmId && m.UserId == userId
        );

        if (member == null)
            throw new UnauthorizedAccessException("User is not a member of this farm.");

        var allowedRoles = new[]
        {
            FarmMemberRoles.Owner,
            FarmMemberRoles.Admin,
            FarmMemberRoles.Editor,
        };
        if (!allowedRoles.Contains(member.Role))
            throw new UnauthorizedAccessException(
                "User does not have permission to add paddocks to this farm."
            );

        var paddock = new Paddock { Name = request.Name, FarmId = request.FarmId };

        await paddockRepository.AddAsync(paddock);
        await unitOfWork.SaveChangesAsync();

        return new PaddockDto
        {
            Id = paddock.Id,
            Name = paddock.Name,
            FarmId = paddock.FarmId,
            FarmName = farm.Name,
            CreatedAt = paddock.CreatedAt,
        };
    }
}
