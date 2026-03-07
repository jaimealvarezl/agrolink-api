using AgroLink.Application.Features.Movements.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Movements.Queries.GetMovementsByEntity;

public class GetMovementsByEntityQueryHandler(
    IMovementRepository movementRepository,
    IUserRepository userRepository,
    IAnimalRepository animalRepository,
    ILotRepository lotRepository,
    IPaddockRepository paddockRepository
) : IRequestHandler<GetMovementsByEntityQuery, IEnumerable<MovementDto>>
{
    public async Task<IEnumerable<MovementDto>> Handle(
        GetMovementsByEntityQuery request,
        CancellationToken cancellationToken
    )
    {
        var movements = await movementRepository.GetMovementsByEntityAsync(
            request.EntityType,
            request.EntityId
        );
        var movementList = movements.ToList();

        if (!movementList.Any())
            return new List<MovementDto>();

        // Gather all unique IDs to fetch them efficiently
        var userIds = movementList.Select(m => m.UserId).Distinct().ToList();

        // Fetch Users
        var users = await userRepository.FindAsync(u => userIds.Contains(u.Id));
        var usersDict = users.ToDictionary(u => u.Id, u => u.Name);

        // Pre-fetch entities depending on type
        var animalIds = new HashSet<int>();
        var lotIds = new HashSet<int>();
        var paddockIds = new HashSet<int>();

        foreach (var m in movementList)
        {
            if (m.EntityType == "ANIMAL")
            {
                animalIds.Add(m.EntityId);
                if (m.FromId.HasValue)
                    lotIds.Add(m.FromId.Value);
                if (m.ToId.HasValue)
                    lotIds.Add(m.ToId.Value);
            }
            else if (m.EntityType == "LOT")
            {
                lotIds.Add(m.EntityId);
                if (m.FromId.HasValue)
                    paddockIds.Add(m.FromId.Value);
                if (m.ToId.HasValue)
                    paddockIds.Add(m.ToId.Value);
            }
        }

        var animalsDict = new Dictionary<int, string>();
        if (animalIds.Any())
        {
            var animals = await animalRepository.FindAsync(a => animalIds.Contains(a.Id));
            animalsDict = animals.ToDictionary(a => a.Id, a => a.TagVisual ?? "");
        }

        var lotsDict = new Dictionary<int, string>();
        if (lotIds.Any())
        {
            var lots = await lotRepository.FindAsync(l => lotIds.Contains(l.Id));
            lotsDict = lots.ToDictionary(l => l.Id, l => l.Name);
        }

        var paddocksDict = new Dictionary<int, string>();
        if (paddockIds.Any())
        {
            // Assuming paddockRepository has FindAsync
            var paddocks = await paddockRepository.FindAsync(p => paddockIds.Contains(p.Id));
            paddocksDict = paddocks.ToDictionary(p => p.Id, p => p.Name);
        }

        var result = new List<MovementDto>();

        foreach (var movement in movementList)
        {
            string? entityName = null;
            string? fromName = null;
            string? toName = null;

            if (movement.EntityType == "ANIMAL")
            {
                entityName = animalsDict.GetValueOrDefault(movement.EntityId);
                if (movement.FromId.HasValue)
                    fromName = lotsDict.GetValueOrDefault(movement.FromId.Value);
                if (movement.ToId.HasValue)
                    toName = lotsDict.GetValueOrDefault(movement.ToId.Value);
            }
            else if (movement.EntityType == "LOT")
            {
                entityName = lotsDict.GetValueOrDefault(movement.EntityId);
                if (movement.FromId.HasValue)
                    fromName = paddocksDict.GetValueOrDefault(movement.FromId.Value);
                if (movement.ToId.HasValue)
                    toName = paddocksDict.GetValueOrDefault(movement.ToId.Value);
            }

            result.Add(
                new MovementDto
                {
                    Id = movement.Id,
                    EntityType = movement.EntityType,
                    EntityId = movement.EntityId,
                    EntityName = entityName,
                    FromId = movement.FromId,
                    FromName = fromName,
                    ToId = movement.ToId,
                    ToName = toName,
                    At = movement.At,
                    Reason = movement.Reason,
                    UserId = movement.UserId,
                    UserName = usersDict.GetValueOrDefault(movement.UserId, ""),
                    CreatedAt = movement.CreatedAt,
                }
            );
        }

        return result.OrderByDescending(m => m.At);
    }
}
