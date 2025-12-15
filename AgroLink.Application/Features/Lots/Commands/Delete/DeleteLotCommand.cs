using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Lots.Commands.Delete;

public record DeleteLotCommand(int Id) : IRequest;

public class DeleteLotCommandHandler(ILotRepository lotRepository)
    : IRequestHandler<DeleteLotCommand>
{
    public async Task Handle(DeleteLotCommand request, CancellationToken cancellationToken)
    {
        var lot = await lotRepository.GetByIdAsync(request.Id);
        if (lot == null)
        {
            throw new ArgumentException("Lot not found");
        }

        lotRepository.Remove(lot);
    }
}
