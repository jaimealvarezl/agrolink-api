using AgroLink.Application.Features.Checklists.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Checklists.Queries.GetByLot;

public record GetChecklistsByLotQuery(int LotId) : IRequest<IEnumerable<ChecklistDto>>;
