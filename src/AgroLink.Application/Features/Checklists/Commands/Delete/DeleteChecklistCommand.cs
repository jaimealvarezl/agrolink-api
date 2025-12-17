using MediatR;

namespace AgroLink.Application.Features.Checklists.Commands.Delete;

public record DeleteChecklistCommand(int Id) : IRequest<Unit>;
