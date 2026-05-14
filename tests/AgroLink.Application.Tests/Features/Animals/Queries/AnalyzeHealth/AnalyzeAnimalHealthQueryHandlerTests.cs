using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Animals.Models;
using AgroLink.Application.Features.Animals.Queries.AnalyzeHealth;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Queries.AnalyzeHealth;

[TestFixture]
public class AnalyzeAnimalHealthQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<AnalyzeAnimalHealthQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private AnalyzeAnimalHealthQueryHandler _handler = null!;

    private const int AnimalId = 1;
    private const int FarmId = 100;
    private const int UserId = 10;

    private static Animal BuildAnimal(bool withPhoto = true)
    {
        return new Animal
        {
            Id = AnimalId,
            Name = "Rosita",
            Breed = "Brahman",
            Sex = Sex.Female,
            BirthDate = DateTime.UtcNow.AddYears(-3),
            ProductionStatus = ProductionStatus.Milking,
            ReproductiveStatus = ReproductiveStatus.Open,
            Lot = new Lot { Paddock = new Paddock { FarmId = FarmId } },
            Photos = withPhoto
                ?
                [
                    new AnimalPhoto
                    {
                        StorageKey = "farm/1/photo.jpg",
                        ContentType = "image/jpeg",
                        IsProfile = true,
                        UploadedAt = DateTime.UtcNow,
                    },
                ]
                : [],
        };
    }

    private static AnimalHealthAnalysisResult SuccessResult(
        double bcs = 3.0,
        bool hasAlert = false,
        string? alertDescription = null
    )
    {
        return new AnimalHealthAnalysisResult
        {
            BodyConditionScore = bcs,
            HasAlert = hasAlert,
            AlertDescription = alertDescription,
            PhotoRejected = false,
            RawAiResponse = "{}",
        };
    }

    private static AnimalHealthAnalysisResult RejectedResult()
    {
        return new AnimalHealthAnalysisResult
        {
            PhotoRejected = true,
            RejectionReason = "Imagen borrosa",
            RawAiResponse = "{}",
        };
    }

    [Test]
    public async Task Handle_ValidAnimalWithPhoto_ReturnsMappedDto()
    {
        var animal = BuildAnimal();
        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetAnimalDetailsAsync(AnimalId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(animal);
        _mocker
            .GetMock<IAnimalHealthAnalysisService>()
            .Setup(s =>
                s.AnalyzeAsync(
                    It.IsAny<AnimalHealthAnalysisRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(SuccessResult(3.5, true, "Garrapatas en lomo."));

        var result = await _handler.Handle(
            new AnalyzeAnimalHealthQuery(AnimalId, FarmId, UserId),
            CancellationToken.None
        );

        result.EstimatedBcs.ShouldBe(3.5);
        result.HasAlerts.ShouldBeTrue();
        result.AlertDescription.ShouldBe("Garrapatas en lomo.");
    }

    [Test]
    public async Task Handle_NoAlert_ReturnsFalseAndNullDescription()
    {
        var animal = BuildAnimal();
        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetAnimalDetailsAsync(AnimalId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(animal);
        _mocker
            .GetMock<IAnimalHealthAnalysisService>()
            .Setup(s =>
                s.AnalyzeAsync(
                    It.IsAny<AnimalHealthAnalysisRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(SuccessResult(4.0));

        var result = await _handler.Handle(
            new AnalyzeAnimalHealthQuery(AnimalId, FarmId, UserId),
            CancellationToken.None
        );

        result.HasAlerts.ShouldBeFalse();
        result.AlertDescription.ShouldBeNull();
    }

    [Test]
    public async Task Handle_AnimalNotFound_ThrowsNotFoundException()
    {
        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetAnimalDetailsAsync(AnimalId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Animal?)null);

        await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(
                new AnalyzeAnimalHealthQuery(AnimalId, FarmId, UserId),
                CancellationToken.None
            )
        );
    }

    [Test]
    public async Task Handle_AnimalBelongsToOtherFarm_ThrowsNotFoundException()
    {
        var animal = BuildAnimal();
        animal.Lot.Paddock.FarmId = 999;

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetAnimalDetailsAsync(AnimalId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(animal);

        await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(
                new AnalyzeAnimalHealthQuery(AnimalId, FarmId, UserId),
                CancellationToken.None
            )
        );
    }

    [Test]
    public async Task Handle_AnimalHasNoPhotos_ThrowsArgumentException()
    {
        var animal = BuildAnimal(false);
        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetAnimalDetailsAsync(AnimalId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(animal);

        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(
                new AnalyzeAnimalHealthQuery(AnimalId, FarmId, UserId),
                CancellationToken.None
            )
        );
    }

    [Test]
    public async Task Handle_PhotoRejectedByAi_ThrowsArgumentExceptionWithSpecificMessage()
    {
        var animal = BuildAnimal();
        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetAnimalDetailsAsync(AnimalId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(animal);
        _mocker
            .GetMock<IAnimalHealthAnalysisService>()
            .Setup(s =>
                s.AnalyzeAsync(
                    It.IsAny<AnimalHealthAnalysisRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(RejectedResult());

        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(
                new AnalyzeAnimalHealthQuery(AnimalId, FarmId, UserId),
                CancellationToken.None
            )
        );

        ex.Message.ShouldBe("La imagen no es lo suficientemente clara para realizar un análisis.");
    }

    [Test]
    public async Task Handle_SelectsPrimaryPhotoFirst()
    {
        var animal = BuildAnimal(false);
        var newerNonProfile = new AnimalPhoto
        {
            StorageKey = "newer-non-profile.jpg",
            ContentType = "image/jpeg",
            IsProfile = false,
            UploadedAt = DateTime.UtcNow,
        };
        var profilePhoto = new AnimalPhoto
        {
            StorageKey = "profile.jpg",
            ContentType = "image/jpeg",
            IsProfile = true,
            UploadedAt = DateTime.UtcNow.AddDays(-1),
        };
        animal.Photos = [newerNonProfile, profilePhoto];

        AnimalHealthAnalysisRequest? capturedRequest = null;
        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetAnimalDetailsAsync(AnimalId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(animal);
        _mocker
            .GetMock<IAnimalHealthAnalysisService>()
            .Setup(s =>
                s.AnalyzeAsync(
                    It.IsAny<AnimalHealthAnalysisRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<AnimalHealthAnalysisRequest, CancellationToken>((r, _) => capturedRequest = r)
            .ReturnsAsync(SuccessResult());

        await _handler.Handle(
            new AnalyzeAnimalHealthQuery(AnimalId, FarmId, UserId),
            CancellationToken.None
        );

        capturedRequest.ShouldNotBeNull();
        capturedRequest!.PhotoStorageKey.ShouldBe("profile.jpg");
    }

    [Test]
    public async Task Handle_FallsBackToMostRecentPhotoWhenNoProfilePhoto()
    {
        var animal = BuildAnimal(false);
        var olderPhoto = new AnimalPhoto
        {
            StorageKey = "older.jpg",
            ContentType = "image/jpeg",
            IsProfile = false,
            UploadedAt = DateTime.UtcNow.AddDays(-5),
        };
        var newerPhoto = new AnimalPhoto
        {
            StorageKey = "newer.jpg",
            ContentType = "image/jpeg",
            IsProfile = false,
            UploadedAt = DateTime.UtcNow,
        };
        animal.Photos = [olderPhoto, newerPhoto];

        AnimalHealthAnalysisRequest? capturedRequest = null;
        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetAnimalDetailsAsync(AnimalId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(animal);
        _mocker
            .GetMock<IAnimalHealthAnalysisService>()
            .Setup(s =>
                s.AnalyzeAsync(
                    It.IsAny<AnimalHealthAnalysisRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<AnimalHealthAnalysisRequest, CancellationToken>((r, _) => capturedRequest = r)
            .ReturnsAsync(SuccessResult());

        await _handler.Handle(
            new AnalyzeAnimalHealthQuery(AnimalId, FarmId, UserId),
            CancellationToken.None
        );

        capturedRequest!.PhotoStorageKey.ShouldBe("newer.jpg");
    }

    [Test]
    public async Task Handle_PassesCorrectBioPacketToService()
    {
        var birthDate = DateTime.UtcNow.AddYears(-3);
        var animal = BuildAnimal();
        animal.BirthDate = birthDate;
        animal.Breed = "Angus";
        animal.Sex = Sex.Male;
        animal.ProductionStatus = ProductionStatus.Bull;
        animal.ReproductiveStatus = ReproductiveStatus.NotApplicable;

        AnimalHealthAnalysisRequest? capturedRequest = null;
        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetAnimalDetailsAsync(AnimalId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(animal);
        _mocker
            .GetMock<IAnimalHealthAnalysisService>()
            .Setup(s =>
                s.AnalyzeAsync(
                    It.IsAny<AnimalHealthAnalysisRequest>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<AnimalHealthAnalysisRequest, CancellationToken>((r, _) => capturedRequest = r)
            .ReturnsAsync(SuccessResult());

        await _handler.Handle(
            new AnalyzeAnimalHealthQuery(AnimalId, FarmId, UserId),
            CancellationToken.None
        );

        capturedRequest.ShouldNotBeNull();
        capturedRequest!.Breed.ShouldBe("Angus");
        capturedRequest.Sex.ShouldBe(Sex.Male);
        capturedRequest.BirthDate.ShouldBe(birthDate);
        capturedRequest.ProductionStatus.ShouldBe(ProductionStatus.Bull);
        capturedRequest.ReproductiveStatus.ShouldBe(ReproductiveStatus.NotApplicable);
    }
}
