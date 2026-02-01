using AgroLink.Domain.Enums;

namespace AgroLink.Application.Common.Utilities;

public static class AnimalValidator
{
    public static void ValidateStatusConsistency(
        string sex,
        ProductionStatus productionStatus,
        ReproductiveStatus reproductiveStatus
    )
    {
        var sexUpper = sex.ToUpper();

        // Consistency check for ProductionStatus vs Sex
        if (
            (productionStatus == ProductionStatus.Bull || productionStatus == ProductionStatus.Steer)
            && sexUpper != "MALE"
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
            ) && sexUpper != "FEMALE"
        )
        {
            throw new ArgumentException(
                $"ProductionStatus '{productionStatus}' is only valid for FEMALE animals."
            );
        }

        // Consistency check for ReproductiveStatus vs Sex
        if (sexUpper == "MALE" && reproductiveStatus != ReproductiveStatus.NotApplicable)
        {
            throw new ArgumentException(
                "MALE animals must have ReproductiveStatus set to NotApplicable."
            );
        }

        if (reproductiveStatus == ReproductiveStatus.Pregnant && sexUpper != "FEMALE")
        {
            throw new ArgumentException("Only FEMALE animals can have ReproductiveStatus 'Pregnant'.");
        }
    }
}
