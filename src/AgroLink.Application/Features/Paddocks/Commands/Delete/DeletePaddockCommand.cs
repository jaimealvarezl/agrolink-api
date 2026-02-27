using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Paddocks.Commands.Delete;

public record DeletePaddockCommand(int Id) : IRequest;

public class DeletePaddockCommandHandler(
    IPaddockRepository paddockRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork
) : IRequestHandler<DeletePaddockCommand>
{
    public async Task Handle(DeletePaddockCommand request, CancellationToken cancellationToken)
    {
        var paddock = await paddockRepository.GetByIdAsync(request.Id);
        if (paddock == null)
        {
            throw new ArgumentException("Paddock not found");
        }

        if (
            currentUserService.CurrentFarmId.HasValue
            && paddock.FarmId != currentUserService.CurrentFarmId.Value
        )
        {
            throw new ForbiddenAccessException("You do not have access to this paddock");
        }

        paddockRepository.Remove(paddock);
        await unitOfWork.SaveChangesAsync();
    }
}
