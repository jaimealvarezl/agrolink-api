using System;
using System.Collections.Generic;
using System.Linq;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;

namespace AgroLink.Application.Common.Utilities;

public static class AnimalValidator
{
    public static void ValidateStatusConsistency(
        Sex sex,
        ProductionStatus productionStatus,
        ReproductiveStatus reproductiveStatus
    )
    {
        // Consistency check for ProductionStatus vs Sex
        if (
            (
                productionStatus == ProductionStatus.Bull
                || productionStatus == ProductionStatus.Steer
            )
            && sex != Sex.Male
        )
        {
            throw new ArgumentException(
                $"ProductionStatus '{productionStatus}' is only valid for MALE animals."
            );
        }

        if (
            (
                productionStatus == ProductionStatus.Heifer
                || productionStatus == ProductionStatus.Milking
                || productionStatus == ProductionStatus.Dry
            )
            && sex != Sex.Female
        )
        {
            throw new ArgumentException(
                $"ProductionStatus '{productionStatus}' is only valid for FEMALE animals."
            );
        }

        // Consistency check for ReproductiveStatus vs Sex
        if (sex == Sex.Male && reproductiveStatus != ReproductiveStatus.NotApplicable)
        {
            throw new ArgumentException(
                "MALE animals must have ReproductiveStatus set to NotApplicable."
            );
        }

        if (reproductiveStatus == ReproductiveStatus.Pregnant && sex != Sex.Female)
        {
            throw new ArgumentException(
                "Only FEMALE animals can have ReproductiveStatus 'Pregnant'."
            );
        }
    }

    public static void ValidateOwners(IEnumerable<decimal> shares)
    {
        var total = shares.Sum();
        if (total != 100)
        {
            throw new ArgumentException($"Total ownership percentage must be 100%. Current: {total}%");
        }
    }

    public static void ValidateParentage(Animal? mother, Animal? father, int targetFarmId)
    {
        if (mother != null)
        {
            if (mother.Sex != Sex.Female)
            {
                throw new ArgumentException($"Mother must be Female. Animal {mother.Id} is {mother.Sex}.");
            }

            if (mother.Lot.Paddock.FarmId != targetFarmId)
            {
                throw new ArgumentException($"Mother (ID {mother.Id}) belongs to a different farm.");
            }
        }

        if (father != null)
        {
            if (father.Sex != Sex.Male)
            {
                throw new ArgumentException($"Father must be Male. Animal {father.Id} is {father.Sex}.");
            }

            if (father.Lot.Paddock.FarmId != targetFarmId)
            {
                throw new ArgumentException($"Father (ID {father.Id}) belongs to a different farm.");
            }
        }
    }
}
