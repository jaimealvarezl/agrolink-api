using AgroLink.Application.Features.Checklists.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Checklists.Commands.Create;

public record CreateChecklistCommand(CreateChecklistDto Dto, int UserId) : IRequest<ChecklistDto>;
