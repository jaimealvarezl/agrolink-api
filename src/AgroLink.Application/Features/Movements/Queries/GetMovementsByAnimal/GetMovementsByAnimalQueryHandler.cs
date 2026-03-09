using AgroLink.Application.Features.Movements.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Movements.Queries.GetMovementsByAnimal;

public class GetMovementsByAnimalQueryHandler(
    IMovementRepository movementRepository,
    IUserRepository userRepository,
    IAnimalRepository animalRepository,
    ILotRepository lotRepository
) : IRequestHandler<GetMovementsByAnimalQuery, IEnumerable<MovementDto>>
{
    public async Task<IEnumerable<MovementDto>> Handle(
        GetMovementsByAnimalQuery request,
        CancellationToken cancellationToken
    )
    {
        var movements = await movementRepository.GetMovementsByAnimalAsync(request.AnimalId);
        var movementList = movements.ToList();

        if (movementList.Count == 0)
        {
            return new List<MovementDto>();
        }

        // Gather all unique IDs to fetch them efficiently
        var userIds = movementList.Select(m => m.UserId).Distinct().ToList();
        var lotIds = movementList
            .SelectMany(m => new[] { m.FromLotId, m.ToLotId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        // Fetch Users
        var users = await userRepository.FindAsync(u => userIds.Contains(u.Id));
        var usersDict = users.ToDictionary(u => u.Id, u => u.Name);

        // Fetch Animal
        var animal = await animalRepository.GetByIdAsync(request.AnimalId);
        var animalName = animal?.TagVisual;

        // Fetch Lots
        var lotsDict = new Dictionary<int, string>();
        if (lotIds.Any())
        {
            var lots = await lotRepository.FindAsync(l => lotIds.Contains(l.Id));
            lotsDict = lots.ToDictionary(l => l.Id, l => l.Name);
        }

        var result = movementList
            .Select(movement => new MovementDto
            {
                Id = movement.Id,
                AnimalId = movement.AnimalId,
                AnimalName = animalName,
                FromLotId = movement.FromLotId,
                FromLotName = movement.FromLotId.HasValue
                    ? lotsDict.GetValueOrDefault(movement.FromLotId.Value)
                    : null,
                ToLotId = movement.ToLotId,
                ToLotName = movement.ToLotId.HasValue
                    ? lotsDict.GetValueOrDefault(movement.ToLotId.Value)
                    : null,
                At = movement.At,
                Reason = movement.Reason,
                UserId = movement.UserId,
                UserName = usersDict.GetValueOrDefault(movement.UserId, ""),
                CreatedAt = movement.CreatedAt,
            })
            .ToList();

        return result.OrderByDescending(m => m.At);
    }
}
