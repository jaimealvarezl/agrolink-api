using AgroLink.Application.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Checklists.Queries.GetById;

public record GetChecklistByIdQuery(int Id) : IRequest<ChecklistDto?>;
