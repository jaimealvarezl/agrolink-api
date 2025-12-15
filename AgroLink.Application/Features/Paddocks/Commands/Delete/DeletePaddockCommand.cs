using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Paddocks.Commands.Delete;

public record DeletePaddockCommand(int Id) : IRequest;

public class DeletePaddockCommandHandler(IPaddockRepository paddockRepository)
    : IRequestHandler<DeletePaddockCommand>
{
    public async Task Handle(DeletePaddockCommand request, CancellationToken cancellationToken)
    {
        var paddock = await paddockRepository.GetByIdAsync(request.Id);
        if (paddock == null)
        {
            throw new ArgumentException("Paddock not found");
        }

        paddockRepository.Remove(paddock);
    }
}
