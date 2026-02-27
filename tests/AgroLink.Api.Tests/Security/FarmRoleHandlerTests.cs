using System.Security.Claims;
using AgroLink.Api.Security;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Moq;
using Shouldly;

namespace AgroLink.Api.Tests.Security;

[TestFixture]
public class FarmRoleHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _cacheMock = new Mock<IMemoryCache>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeMock = new Mock<IServiceScope>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _farmMemberRepoMock = new Mock<IFarmMemberRepository>();
        _cacheEntryMock = new Mock<ICacheEntry>();

        // Setup Service Provider and Scope
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_scopeFactoryMock.Object);
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IFarmMemberRepository)))
            .Returns(_farmMemberRepoMock.Object);

        // Setup Cache
        _cacheMock.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(_cacheEntryMock.Object);

        _handler = new FarmRoleHandler(
            _cacheMock.Object,
            _serviceProviderMock.Object,
            _httpContextAccessorMock.Object
        );
    }

    private Mock<IMemoryCache> _cacheMock = null!;
    private Mock<IServiceProvider> _serviceProviderMock = null!;
    private Mock<IServiceScopeFactory> _scopeFactoryMock = null!;
    private Mock<IServiceScope> _scopeMock = null!;
    private Mock<IHttpContextAccessor> _httpContextAccessorMock = null!;
    private Mock<IFarmMemberRepository> _farmMemberRepoMock = null!;
    private Mock<ICacheEntry> _cacheEntryMock = null!;
    private FarmRoleHandler _handler = null!;

    [Test]
    public async Task HandleAsync_WhenUserHasRequiredRole_ShouldSucceed()
    {
        // Arrange
        var userId = 123;
        var farmId = 1;
        var role = FarmMemberRoles.Owner;
        var requirement = new FarmRoleRequirement(FarmMemberRoles.Owner);

        SetupHttpContext(userId, farmId);
        SetupRepo(userId, farmId, role);

        var context = new AuthorizationHandlerContext([requirement], SetupUser(userId), null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    [Test]
    public async Task HandleAsync_WhenUserRoleIsHigher_ShouldSucceed()
    {
        // Arrange
        var userId = 123;
        var farmId = 1;
        var requirement = new FarmRoleRequirement(FarmMemberRoles.Viewer);

        SetupHttpContext(userId, farmId);
        SetupRepo(userId, farmId, FarmMemberRoles.Admin); // Admin > Viewer

        var context = new AuthorizationHandlerContext([requirement], SetupUser(userId), null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    [Test]
    public async Task HandleAsync_WhenUserRoleIsLower_ShouldFail()
    {
        // Arrange
        var userId = 123;
        var farmId = 1;
        var requirement = new FarmRoleRequirement(FarmMemberRoles.Owner);

        SetupHttpContext(userId, farmId);
        SetupRepo(userId, farmId, FarmMemberRoles.Viewer); // Viewer < Owner

        var context = new AuthorizationHandlerContext([requirement], SetupUser(userId), null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeFalse();
    }

    [Test]
    public async Task HandleAsync_ShouldExtractFarmIdFromRoute()
    {
        // Arrange
        var userId = 123;
        var farmId = 55;
        var requirement = new FarmRoleRequirement(FarmMemberRoles.Viewer);

        SetupHttpContext(userId, farmId);
        SetupRepo(userId, farmId, FarmMemberRoles.Viewer);

        var context = new AuthorizationHandlerContext([requirement], SetupUser(userId), null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
        _httpContextAccessorMock.Object.HttpContext!.Items["CurrentFarmId"].ShouldBe(farmId);
    }

    [Test]
    public async Task HandleAsync_ShouldExtractFarmIdFromHeader()
    {
        // Arrange
        var userId = 123;
        var farmId = 77;
        var requirement = new FarmRoleRequirement(FarmMemberRoles.Viewer);

        SetupHttpContext(userId, farmId, "header");
        SetupRepo(userId, farmId, FarmMemberRoles.Viewer);

        var context = new AuthorizationHandlerContext([requirement], SetupUser(userId), null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
        _httpContextAccessorMock.Object.HttpContext!.Items["CurrentFarmId"].ShouldBe(farmId);
    }

    private void SetupHttpContext(int userId, int farmId, string source = "route")
    {
        var httpContext = new DefaultHttpContext();
        if (source == "route")
        {
            httpContext.Request.RouteValues["farmId"] = farmId.ToString();
        }
        else if (source == "query")
        {
            httpContext.Request.Query = new QueryCollection(
                new Dictionary<string, StringValues> { { "farmId", farmId.ToString() } }
            );
        }
        else if (source == "header")
        {
            httpContext.Request.Headers["x-farm-id"] = farmId.ToString();
        }

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
    }

    private ClaimsPrincipal SetupUser(int userId)
    {
        var claims = new List<Claim> { new("userid", userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "test");
        return new ClaimsPrincipal(identity);
    }

    private void SetupRepo(int userId, int farmId, string role)
    {
        object? cacheValue = null;
        _cacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue)).Returns(false);

        _farmMemberRepoMock
            .Setup(x => x.GetByFarmAndUserAsync(farmId, userId))
            .ReturnsAsync(
                new FarmMember
                {
                    Role = role,
                    UserId = userId,
                    FarmId = farmId,
                }
            );
    }
}
