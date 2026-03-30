using System.Linq.Expressions;
using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.OwnerBrands.Commands.Create;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.OwnerBrands.Commands.Create;

[TestFixture]
public class CreateOwnerBrandCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<CreateOwnerBrandCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private CreateOwnerBrandCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidCommand_CreatesBrand()
    {
        // Arrange
        var command = new CreateOwnerBrandCommand(1, 10, "Tres rayas");

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(true);

        _mocker
            .GetMock<IOwnerBrandRepository>()
            .Setup(r => r.AddAsync(It.IsAny<OwnerBrand>()))
            .Callback<OwnerBrand, CancellationToken>((b, _) => b.Id = 5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(5);
        result.OwnerId.ShouldBe(10);
        result.Description.ShouldBe("Tres rayas");
        result.PhotoUrl.ShouldBeNull();
        result.IsActive.ShouldBeTrue();

        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
        // No photo on creation — presigned URL should not be called
        _mocker
            .GetMock<IStorageService>()
            .Verify(s => s.GetPresignedUrl(It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Test]
    public async Task Handle_OwnerNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var command = new CreateOwnerBrandCommand(1, 99, "Brand");

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }
}
