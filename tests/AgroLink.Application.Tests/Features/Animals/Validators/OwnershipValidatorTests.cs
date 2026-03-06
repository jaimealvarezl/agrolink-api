using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Features.Animals.Validators;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Validators;

[TestFixture]
public class OwnershipValidatorTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _validator = _mocker.CreateInstance<OwnershipValidator>();
    }

    private AutoMocker _mocker = null!;
    private OwnershipValidator _validator = null!;

    [Test]
    public async Task ValidateAsync_SingleOwnerWith100Percent_Passes()
    {
        var targetFarmId = 10;
        var owners = new List<AnimalOwnerCreateDto>
        {
            new() { OwnerId = 1, SharePercent = 100 },
        };

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(
                new Owner
                {
                    Id = 1,
                    FarmId = targetFarmId,
                    IsActive = true,
                }
            );

        await Should.NotThrowAsync(() => _validator.ValidateAsync(owners, targetFarmId));
    }

    [Test]
    public async Task ValidateAsync_MultipleActiveOwnersSumming100Percent_Passes()
    {
        var targetFarmId = 10;
        var owners = new List<AnimalOwnerCreateDto>
        {
            new() { OwnerId = 1, SharePercent = 60 },
            new() { OwnerId = 2, SharePercent = 40 },
        };

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(
                new Owner
                {
                    Id = 1,
                    FarmId = targetFarmId,
                    IsActive = true,
                }
            );
        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.GetByIdAsync(2))
            .ReturnsAsync(
                new Owner
                {
                    Id = 2,
                    FarmId = targetFarmId,
                    IsActive = true,
                }
            );

        await Should.NotThrowAsync(() => _validator.ValidateAsync(owners, targetFarmId));
    }

    [TestCase(99.9)]
    [TestCase(100.1)]
    public async Task ValidateAsync_SumIsNot100Percent_ThrowsArgumentException(double share)
    {
        var targetFarmId = 10;
        var owners = new List<AnimalOwnerCreateDto>
        {
            new() { OwnerId = 1, SharePercent = (decimal)share },
        };

        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _validator.ValidateAsync(owners, targetFarmId)
        );
        ex.Message.ShouldContain("Total ownership percentage must be exactly 100%");
    }

    [Test]
    public async Task ValidateAsync_OwnerBelongsToDifferentFarm_ThrowsArgumentException()
    {
        var targetFarmId = 10;
        var differentFarmId = 20;
        var owners = new List<AnimalOwnerCreateDto>
        {
            new() { OwnerId = 1, SharePercent = 100 },
        };

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(
                new Owner
                {
                    Id = 1,
                    FarmId = differentFarmId,
                    IsActive = true,
                }
            );

        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _validator.ValidateAsync(owners, targetFarmId)
        );
        ex.Message.ShouldContain("does not belong to the target farm");
    }

    [Test]
    public async Task ValidateAsync_OwnerIsArchived_ThrowsArgumentException()
    {
        var targetFarmId = 10;
        var owners = new List<AnimalOwnerCreateDto>
        {
            new() { OwnerId = 1, SharePercent = 100 },
        };

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(
                new Owner
                {
                    Id = 1,
                    FarmId = targetFarmId,
                    IsActive = false,
                }
            );

        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _validator.ValidateAsync(owners, targetFarmId)
        );
        ex.Message.ShouldContain("is archived and cannot receive new ownership shares");
    }

    [Test]
    public async Task ValidateAsync_OwnerNotFound_ThrowsArgumentException()
    {
        var targetFarmId = 10;
        var owners = new List<AnimalOwnerCreateDto>
        {
            new() { OwnerId = 1, SharePercent = 100 },
        };

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync((Owner?)null);

        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            _validator.ValidateAsync(owners, targetFarmId)
        );
        ex.Message.ShouldContain("not found");
    }
}
