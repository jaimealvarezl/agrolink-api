using AgroLink.Application.Common.Utilities;
using AgroLink.Domain.Enums;
using Shouldly;

namespace AgroLink.Application.Tests.Common.Utilities;

[TestFixture]
public class AnimalValidatorTests
{
    [Test]
    [TestCase(Sex.Male, ProductionStatus.Bull, ReproductiveStatus.NotApplicable)]
    [TestCase(Sex.Male, ProductionStatus.Steer, ReproductiveStatus.NotApplicable)]
    [TestCase(Sex.Female, ProductionStatus.Heifer, ReproductiveStatus.Open)]
    [TestCase(Sex.Female, ProductionStatus.Milking, ReproductiveStatus.Pregnant)]
    [TestCase(Sex.Female, ProductionStatus.Dry, ReproductiveStatus.Open)]
    [TestCase(Sex.Female, ProductionStatus.Calf, ReproductiveStatus.NotApplicable)]
    public void ValidateStatusConsistency_ValidCombinations_DoesNotThrow(
        Sex sex,
        ProductionStatus prod,
        ReproductiveStatus repro
    )
    {
        // Act & Assert

        Should.NotThrow(() => AnimalValidator.ValidateStatusConsistency(sex, prod, repro));
    }

    [Test]
    [TestCase(Sex.Female, ProductionStatus.Bull, ReproductiveStatus.Open)]
    [TestCase(Sex.Female, ProductionStatus.Steer, ReproductiveStatus.Open)]
    public void ValidateStatusConsistency_MaleOnlyProductionStatusForFemale_ThrowsArgumentException(
        Sex sex,
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
    [TestCase(Sex.Male, ProductionStatus.Heifer, ReproductiveStatus.NotApplicable)]
    [TestCase(Sex.Male, ProductionStatus.Milking, ReproductiveStatus.NotApplicable)]
    public void ValidateStatusConsistency_FemaleOnlyProductionStatusForMale_ThrowsArgumentException(
        Sex sex,
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
                Sex.Male,
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
                Sex.Female,
                ProductionStatus.Heifer,
                ReproductiveStatus.Pregnant
            )
        );
    }
}
