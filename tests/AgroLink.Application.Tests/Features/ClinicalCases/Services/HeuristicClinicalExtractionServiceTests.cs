using AgroLink.Application.Common.Services;
using AgroLink.Domain.Enums;
using Shouldly;

namespace AgroLink.Application.Tests.Features.ClinicalCases.Services;

[TestFixture]
public class HeuristicClinicalExtractionServiceTests
{
    [Test]
    public async Task ExtractAsync_WithFarmAnimalAndEarTag_ShouldExtractExpectedFields()
    {
        // Arrange
        var service = new HeuristicClinicalExtractionService();
        var message = "granja: El Rosario animal: Lola arete: A-123 sintomas: tos y fiebre";

        // Act
        var result = await service.ExtractAsync(message);

        // Assert
        result.FarmReference.ShouldBe("El Rosario");
        result.AnimalReference.ShouldBe("Lola");
        result.EarTag.ShouldBe("A-123");
        result.Intent.ShouldBe(ClinicalMessageIntent.NewCaseReport);
    }

    [Test]
    public async Task ExtractAsync_WithStatusMessage_ShouldReturnAnimalStatusIntent()
    {
        // Arrange
        var service = new HeuristicClinicalExtractionService();
        var message = "estado granja: El Rosario animal: Lola";

        // Act
        var result = await service.ExtractAsync(message);

        // Assert
        result.Intent.ShouldBe(ClinicalMessageIntent.AnimalStatusRequest);
    }
}
