using AgroLink.Application.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Checklists.Commands.Update;

public record UpdateChecklistCommand(int Id, CreateChecklistDto Dto) : IRequest<ChecklistDto>;
