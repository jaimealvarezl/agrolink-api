using System.Linq.Expressions;
using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.OwnerBrands.Commands.Update;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.OwnerBrands.Commands.Update;

[TestFixture]
public class UpdateOwnerBrandCommandHandlerTests
{
    private AutoMocker _mocker = null!;
    private UpdateOwnerBrandCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<UpdateOwnerBrandCommandHandler>();
    }

    [Test]
    public async Task Handle_ValidCommand_UpdatesBrand()
    {
        // Arrange
        var command = new UpdateOwnerBrandCommand(1, 10, 5, "REG-NEW", "Updated desc", "https://new-photo.jpg");
        var existing = new OwnerBrand
        {
            Id = 5,
            OwnerId = 10,
            RegistrationNumber = "REG-OLD",
            Description = "Old desc",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
        };

        _mocker.GetMock<IOwnerRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(true);

        _mocker.GetMock<IOwnerBrandRepository>()
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<OwnerBrand, bool>>>()))
            .ReturnsAsync(existing);

        _mocker.GetMock<IOwnerBrandRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<OwnerBrand, bool>>>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.RegistrationNumber.ShouldBe("REG-NEW");
        result.Description.ShouldBe("Updated desc");
        result.PhotoUrl.ShouldBe("https://new-photo.jpg");
        result.UpdatedAt.ShouldNotBeNull();

        _mocker.GetMock<IOwnerBrandRepository>().Verify(r => r.Update(existing), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_OwnerNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var command = new UpdateOwnerBrandCommand(1, 99, 5, "REG-001", "Brand", null);

        _mocker.GetMock<IOwnerRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_BrandNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var command = new UpdateOwnerBrandCommand(1, 10, 999, "REG-001", "Brand", null);

        _mocker.GetMock<IOwnerRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(true);

        _mocker.GetMock<IOwnerBrandRepository>()
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<OwnerBrand, bool>>>()))
            .ReturnsAsync((OwnerBrand?)null);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_DuplicateRegistrationNumber_ThrowsArgumentException()
    {
        // Arrange
        var command = new UpdateOwnerBrandCommand(1, 10, 5, "REG-TAKEN", "Brand", null);
        var existing = new OwnerBrand { Id = 5, OwnerId = 10, RegistrationNumber = "REG-OLD", Description = "Old", IsActive = true };

        _mocker.GetMock<IOwnerRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(true);

        _mocker.GetMock<IOwnerBrandRepository>()
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<OwnerBrand, bool>>>()))
            .ReturnsAsync(existing);

        _mocker.GetMock<IOwnerBrandRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<OwnerBrand, bool>>>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }
}
