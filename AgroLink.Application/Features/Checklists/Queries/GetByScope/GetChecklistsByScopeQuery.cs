using AgroLink.Application.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Checklists.Queries.GetByScope;

public record GetChecklistsByScopeQuery(string ScopeType, int ScopeId)
    : IRequest<IEnumerable<ChecklistDto>>;
