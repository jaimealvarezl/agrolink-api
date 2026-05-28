using AgroLink.Application.Common.Exceptions;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Tags.Commands.DeleteTag;

public record DeleteTagCommand(int Id, int FarmId, int UserId) : IRequest<int>;

public class DeleteTagCommandHandler(ITagRepository tagRepository)
    : IRequestHandler<DeleteTagCommand, int>
{
    public async Task<int> Handle(DeleteTagCommand request, CancellationToken cancellationToken)
    {
        var existingTag = await tagRepository.GetByIdAsync(request.Id, cancellationToken);
        if (existingTag == null || existingTag.FarmId != request.FarmId)
        {
            throw new NotFoundException($"Tag with ID {request.Id} not found.");
        }

        var (_, affectedAnimals) = await tagRepository.DeleteWithCascadeAsync(
            request.Id,
            cancellationToken
        );
        return affectedAnimals;
    }
}
