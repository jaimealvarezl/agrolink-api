using AgroLink.Application.Features.Movements.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Movements.Queries.GetMovementsByAnimal;

public record GetMovementsByAnimalQuery(int AnimalId) : IRequest<IEnumerable<MovementDto>>;
