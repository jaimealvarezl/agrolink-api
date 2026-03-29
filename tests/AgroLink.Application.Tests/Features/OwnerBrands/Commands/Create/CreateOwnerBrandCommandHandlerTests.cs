using System.Linq.Expressions;
using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.OwnerBrands.Commands.Create;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.OwnerBrands.Commands.Create;

[TestFixture]
public class CreateOwnerBrandCommandHandlerTests
{
    private AutoMocker _mocker = null!;
    private CreateOwnerBrandCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<CreateOwnerBrandCommandHandler>();
    }

    [Test]
    public async Task Handle_ValidCommand_CreatesBrand()
    {
        // Arrange
        var command = new CreateOwnerBrandCommand(1, 10, "REG-001", "Tres rayas", null);

        _mocker.GetMock<IOwnerRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(true);

        _mocker.GetMock<IOwnerBrandRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<OwnerBrand, bool>>>()))
            .ReturnsAsync(false);

        _mocker.GetMock<IOwnerBrandRepository>()
            .Setup(r => r.AddAsync(It.IsAny<OwnerBrand>()))
            .Callback<OwnerBrand>(b => b.Id = 5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(5);
        result.OwnerId.ShouldBe(10);
        result.RegistrationNumber.ShouldBe("REG-001");
        result.Description.ShouldBe("Tres rayas");
        result.PhotoUrl.ShouldBeNull();
        result.IsActive.ShouldBeTrue();

        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_OwnerNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var command = new CreateOwnerBrandCommand(1, 99, "REG-001", "Brand", null);

        _mocker.GetMock<IOwnerRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_DuplicateRegistrationNumber_ThrowsArgumentException()
    {
        // Arrange
        var command = new CreateOwnerBrandCommand(1, 10, "REG-DUP", "Brand", null);

        _mocker.GetMock<IOwnerRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(true);

        _mocker.GetMock<IOwnerBrandRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<OwnerBrand, bool>>>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_WithPhotoUrl_StoresPhotoUrl()
    {
        // Arrange
        var command = new CreateOwnerBrandCommand(1, 10, "REG-003", "Brand with photo", "https://storage/brand.jpg");

        _mocker.GetMock<IOwnerRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(true);

        _mocker.GetMock<IOwnerBrandRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<OwnerBrand, bool>>>()))
            .ReturnsAsync(false);

        _mocker.GetMock<IOwnerBrandRepository>()
            .Setup(r => r.AddAsync(It.IsAny<OwnerBrand>()))
            .Callback<OwnerBrand>(b => b.Id = 6);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.PhotoUrl.ShouldBe("https://storage/brand.jpg");
    }
}
