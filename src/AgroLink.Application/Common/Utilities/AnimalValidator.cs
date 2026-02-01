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
}
