using AgroLink.Application.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Checklists.Queries.GetAll;

public record GetAllChecklistsQuery : IRequest<IEnumerable<ChecklistDto>>;
