using AgroLink.Application.Features.Paddocks.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Paddocks.Commands.Create;

public record CreatePaddockCommand(
    string Name,
    int FarmId,
    int UserId,
    decimal? Area,
    string? AreaType
) : IRequest<PaddockDto>;

public class CreatePaddockCommandHandler(
    IPaddockRepository paddockRepository,
    IFarmRepository farmRepository,
    IFarmMemberRepository farmMemberRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreatePaddockCommand, PaddockDto>
{
    private static readonly string[] _allowedRoles =
    [
        FarmMemberRoles.Owner,
        FarmMemberRoles.Admin,
        FarmMemberRoles.Editor,
    ];

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

        if (!_allowedRoles.Contains(member.Role))
            throw new UnauthorizedAccessException(
                "User does not have permission to add paddocks to this farm."
            );

        if (request.Area.HasValue && string.IsNullOrWhiteSpace(request.AreaType))
        {
            throw new ArgumentException("AreaType is required when Area is specified.");
        }

        if (
            !string.IsNullOrWhiteSpace(request.AreaType)
            && !AreaTypes.All.Contains(request.AreaType)
        )
        {
            throw new ArgumentException(
                $"Invalid AreaType. Valid values are: {string.Join(", ", AreaTypes.All)}"
            );
        }

        var paddock = new Paddock
        {
            Name = request.Name,
            FarmId = request.FarmId,
            Area = request.Area,
            AreaType = request.AreaType,
        };

        await paddockRepository.AddAsync(paddock);
        await unitOfWork.SaveChangesAsync();

        return new PaddockDto
        {
            Id = paddock.Id,
            Name = paddock.Name,
            FarmId = paddock.FarmId,
            FarmName = farm.Name,
            Area = paddock.Area,
            AreaType = paddock.AreaType,
            CreatedAt = paddock.CreatedAt,
        };
    }
}
