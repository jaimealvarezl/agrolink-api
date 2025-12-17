using AgroLink.Application.Features.Checklists.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Checklists.Commands.Update;

public record UpdateChecklistCommand(int Id, CreateChecklistDto Dto) : IRequest<ChecklistDto>;
