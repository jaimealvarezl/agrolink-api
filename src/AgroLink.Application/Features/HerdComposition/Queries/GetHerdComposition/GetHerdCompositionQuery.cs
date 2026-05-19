using AgroLink.Application.Features.HerdComposition.DTOs;
using MediatR;

namespace AgroLink.Application.Features.HerdComposition.Queries.GetHerdComposition;

public record GetHerdCompositionQuery(int FarmId) : IRequest<HerdCompositionDto>;
