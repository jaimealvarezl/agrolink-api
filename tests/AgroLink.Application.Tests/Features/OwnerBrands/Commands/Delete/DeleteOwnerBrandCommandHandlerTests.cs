using System.Linq.Expressions;
using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.OwnerBrands.Commands.Delete;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.OwnerBrands.Commands.Delete;

[TestFixture]
public class DeleteOwnerBrandCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<DeleteOwnerBrandCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private DeleteOwnerBrandCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidCommand_SoftDeletesBrand()
    {
        // Arrange
        var command = new DeleteOwnerBrandCommand(1, 10, 5);
        var brand = new OwnerBrand
        {
            Id = 5,
            OwnerId = 10,
            Description = "Brand",
            IsActive = true,
        };

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(true);

        _mocker
            .GetMock<IOwnerBrandRepository>()
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<OwnerBrand, bool>>>()))
            .ReturnsAsync(brand);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        brand.IsActive.ShouldBeFalse();
        brand.UpdatedAt.ShouldNotBeNull();
        _mocker.GetMock<IOwnerBrandRepository>().Verify(r => r.Update(brand), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
        // No photo — S3 delete should not be called
        _mocker
            .GetMock<IStorageService>()
            .Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Handle_BrandWithPhoto_DeletesPhotoFromS3()
    {
        // Arrange
        var command = new DeleteOwnerBrandCommand(1, 10, 5);
        var brand = new OwnerBrand
        {
            Id = 5,
            OwnerId = 10,
            Description = "Brand",
            IsActive = true,
            PhotoStorageKey = "f/abc/ob/xyz.jpg",
        };

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(true);

        _mocker
            .GetMock<IOwnerBrandRepository>()
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<OwnerBrand, bool>>>()))
            .ReturnsAsync(brand);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        brand.IsActive.ShouldBeFalse();
        _mocker
            .GetMock<IStorageService>()
            .Verify(s => s.DeleteFileAsync("f/abc/ob/xyz.jpg"), Times.Once);
    }

    [Test]
    public async Task Handle_S3DeleteFails_StillCompletes()
    {
        // Arrange
        var command = new DeleteOwnerBrandCommand(1, 10, 5);
        var brand = new OwnerBrand
        {
            Id = 5,
            OwnerId = 10,
            Description = "Brand",
            IsActive = true,
            PhotoStorageKey = "f/abc/ob/xyz.jpg",
        };

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(true);

        _mocker
            .GetMock<IOwnerBrandRepository>()
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<OwnerBrand, bool>>>()))
            .ReturnsAsync(brand);

        _mocker
            .GetMock<IStorageService>()
            .Setup(s => s.DeleteFileAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("S3 unavailable"));

        // Act — should not throw even if S3 fails
        await Should.NotThrowAsync(() => _handler.Handle(command, CancellationToken.None));

        brand.IsActive.ShouldBeFalse();
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_OwnerNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var command = new DeleteOwnerBrandCommand(1, 99, 5);

        _mocker
            .GetMock<IOwnerRepository>()
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
        var command = new DeleteOwnerBrandCommand(1, 10, 999);

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(true);

        _mocker
            .GetMock<IOwnerBrandRepository>()
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<OwnerBrand, bool>>>()))
            .ReturnsAsync((OwnerBrand?)null);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }
}
