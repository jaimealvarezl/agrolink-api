using AgroLink.Application.Features.Auth.Queries.GetUserProfile;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Auth.Queries.GetUserProfile;

[TestFixture]
public class GetUserProfileQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetUserProfileQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetUserProfileQueryHandler _handler = null!;

    [Test]
    public async Task Handle_AuthenticatedUser_ReturnsUserDto()
    {
        var userEntity = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Role = "USER",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
        };

        _mocker.GetMock<ICurrentUserService>().Setup(s => s.UserId).Returns(1);
        _mocker
            .GetMock<IAuthRepository>()
            .Setup(r => r.GetUserByIdAsync(1))
            .ReturnsAsync(userEntity);

        var result = await _handler.Handle(new GetUserProfileQuery(), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Email.ShouldBe("test@example.com");
    }

    [Test]
    public async Task Handle_NoAuthenticatedUser_ReturnsNull()
    {
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.UserId).Returns((int?)null);

        var result = await _handler.Handle(new GetUserProfileQuery(), CancellationToken.None);

        result.ShouldBeNull();
        _mocker
            .GetMock<IAuthRepository>()
            .Verify(r => r.GetUserByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task Handle_UserNotFoundInRepository_ReturnsNull()
    {
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.UserId).Returns(1);
        _mocker
            .GetMock<IAuthRepository>()
            .Setup(r => r.GetUserByIdAsync(1))
            .ReturnsAsync((User?)null);

        var result = await _handler.Handle(new GetUserProfileQuery(), CancellationToken.None);

        result.ShouldBeNull();
    }

    [Test]
    public async Task Handle_InactiveUser_ReturnsNull()
    {
        var inactiveUser = new User { Id = 1, IsActive = false };

        _mocker.GetMock<ICurrentUserService>().Setup(s => s.UserId).Returns(1);
        _mocker
            .GetMock<IAuthRepository>()
            .Setup(r => r.GetUserByIdAsync(1))
            .ReturnsAsync(inactiveUser);

        var result = await _handler.Handle(new GetUserProfileQuery(), CancellationToken.None);

        result.ShouldBeNull();
    }
}
