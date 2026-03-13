using AgroLink.Application.Features.Checklists.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Checklists.Queries.GetByScope;

public record GetChecklistsByLotQuery(int LotId) : IRequest<IEnumerable<ChecklistDto>>;
