using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Domain.Interfaces;

namespace AgroLink.Application.Features.Animals.Validators;

public class OwnershipValidator(IOwnerRepository ownerRepository) : IOwnershipValidator
{
    public async Task ValidateAsync(List<AnimalOwnerCreateDto> owners, int targetFarmId)
    {
        var total = owners.Sum(o => o.SharePercent);
        if (total != 100)
        {
            throw new ArgumentException(
                $"Total ownership percentage must be exactly 100%. Current: {total}%"
            );
        }

        var ownerIds = owners.Select(o => o.OwnerId).Distinct().ToList();
        var existingOwners = await ownerRepository.FindAsync(o => ownerIds.Contains(o.Id));
        var ownersDict = existingOwners.ToDictionary(o => o.Id);

        foreach (var ownerDto in owners)
        {
            if (!ownersDict.TryGetValue(ownerDto.OwnerId, out var owner))
            {
                throw new ArgumentException($"Owner with ID {ownerDto.OwnerId} not found.");
            }

            if (owner.FarmId != targetFarmId)
            {
                throw new ArgumentException(
                    $"Owner (ID {ownerDto.OwnerId}) does not belong to the target farm."
                );
            }

            if (!owner.IsActive)
            {
                throw new ArgumentException(
                    $"Owner (ID {ownerDto.OwnerId}) is archived and cannot receive new ownership shares."
                );
            }
        }
    }
}
