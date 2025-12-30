using AgroLink.Application.Features.Paddocks.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Paddocks.Commands.Update;

public record UpdatePaddockCommand(
    int Id,
    string? Name,
    int? FarmId,
    decimal? Area,
    string? AreaType
) : IRequest<PaddockDto>;

public class UpdatePaddockCommandHandler(
    IPaddockRepository paddockRepository,
    IFarmRepository farmRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<UpdatePaddockCommand, PaddockDto>
{
    public async Task<PaddockDto> Handle(
        UpdatePaddockCommand request,
        CancellationToken cancellationToken
    )
    {
        var paddock = await paddockRepository.GetByIdAsync(request.Id);
        if (paddock == null)
        {
            throw new ArgumentException("Paddock not found");
        }

        if (request.Area.HasValue || !string.IsNullOrWhiteSpace(request.AreaType))
        {
            var targetArea = request.Area ?? paddock.Area;
            var targetAreaType = request.AreaType ?? paddock.AreaType;

            if (targetArea.HasValue && string.IsNullOrWhiteSpace(targetAreaType))
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
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            paddock.Name = request.Name;
        }

        if (request.FarmId.HasValue)
        {
            paddock.FarmId = request.FarmId.Value;
        }

        if (request.Area.HasValue)
        {
            paddock.Area = request.Area.Value;
        }

        if (request.AreaType != null)
        {
            paddock.AreaType = request.AreaType;
        }

        paddock.UpdatedAt = DateTime.UtcNow;

        paddockRepository.Update(paddock);
        await unitOfWork.SaveChangesAsync();

        var farm = await farmRepository.GetByIdAsync(paddock.FarmId);

        return new PaddockDto
        {
            Id = paddock.Id,
            Name = paddock.Name,
            FarmId = paddock.FarmId,
            FarmName = farm?.Name ?? "",
            Area = paddock.Area,
            AreaType = paddock.AreaType,
            CreatedAt = paddock.CreatedAt,
        };
    }
}
