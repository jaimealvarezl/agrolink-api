using AgroLink.Application.Common.Utilities;
using AgroLink.Domain.Enums;
using Shouldly;

namespace AgroLink.Application.Tests.Common.Utilities;

[TestFixture]
public class AnimalValidatorTests
{
    [Test]
    [TestCase("MALE", ProductionStatus.Bull, ReproductiveStatus.NotApplicable)]
    [TestCase("MALE", ProductionStatus.Steer, ReproductiveStatus.NotApplicable)]
    [TestCase("FEMALE", ProductionStatus.Heifer, ReproductiveStatus.Open)]
    [TestCase("FEMALE", ProductionStatus.Milking, ReproductiveStatus.Pregnant)]
    [TestCase("FEMALE", ProductionStatus.Dry, ReproductiveStatus.Open)]
    [TestCase("FEMALE", ProductionStatus.Calf, ReproductiveStatus.NotApplicable)]
    public void ValidateStatusConsistency_ValidCombinations_DoesNotThrow(
        string sex,
        ProductionStatus prod,
        ReproductiveStatus repro
    )
    {
        // Act & Assert
        Should.NotThrow(() => AnimalValidator.ValidateStatusConsistency(sex, prod, repro));
    }

    [Test]
    [TestCase("FEMALE", ProductionStatus.Bull, ReproductiveStatus.Open)]
    [TestCase("FEMALE", ProductionStatus.Steer, ReproductiveStatus.Open)]
    public void ValidateStatusConsistency_MaleOnlyProductionStatusForFemale_ThrowsArgumentException(
        string sex,
        ProductionStatus prod,
        ReproductiveStatus repro
    )
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() =>
            AnimalValidator.ValidateStatusConsistency(sex, prod, repro)
        );
        ex.Message.ShouldContain("only valid for MALE");
    }

    [Test]
    [TestCase("MALE", ProductionStatus.Heifer, ReproductiveStatus.NotApplicable)]
    [TestCase("MALE", ProductionStatus.Milking, ReproductiveStatus.NotApplicable)]
    public void ValidateStatusConsistency_FemaleOnlyProductionStatusForMale_ThrowsArgumentException(
        string sex,
        ProductionStatus prod,
        ReproductiveStatus repro
    )
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() =>
            AnimalValidator.ValidateStatusConsistency(sex, prod, repro)
        );
        ex.Message.ShouldContain("only valid for FEMALE");
    }

    [Test]
    public void ValidateStatusConsistency_MalePregnant_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() =>
            AnimalValidator.ValidateStatusConsistency(
                "MALE",
                ProductionStatus.Bull,
                ReproductiveStatus.Pregnant
            )
        );
        ex.Message.ShouldContain("ReproductiveStatus set to NotApplicable");
    }

    [Test]
    public void ValidateStatusConsistency_FemalePregnantProductionStatusCheck_DoesNotThrowIfFemale()
    {
        // Act & Assert
        Should.NotThrow(() =>
            AnimalValidator.ValidateStatusConsistency(
                "FEMALE",
                ProductionStatus.Heifer,
                ReproductiveStatus.Pregnant
            )
        );
    }
}
