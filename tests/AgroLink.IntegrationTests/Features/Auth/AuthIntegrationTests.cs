using System.Net;
using System.Net.Http.Json;
using AgroLink.Application.Features.Auth.DTOs;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.Auth;

public class AuthIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task Register_WithValidData_ShouldCreateUserAndReturnToken()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "New User",
            Email = "newuser@example.com",
            Password = "SecurePassword123",
            Role = "USER"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Auth/register", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        authResponse.ShouldNotBeNull();
        authResponse.Token.ShouldNotBeNullOrEmpty();
        authResponse.User.Email.ShouldBe(request.Email);
    }

    [Test]
    public async Task Login_WithCorrectCredentials_ShouldReturnToken()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Name = "Login User",
            Email = "login@example.com",
            Password = "SecurePassword123",
            Role = "USER"
        };
        await Client.PostAsJsonAsync("/api/Auth/register", registerRequest);

        var loginDto = new LoginDto
        {
            Email = registerRequest.Email,
            Password = registerRequest.Password
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Auth/login", loginDto);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        authResponse.ShouldNotBeNull();
        authResponse.Token.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public async Task GetProfile_WithValidToken_ShouldReturnUser()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Name = "Profile User",
            Email = "profile@example.com",
            Password = "SecurePassword123"
        };
        var regResponse = await Client.PostAsJsonAsync("/api/Auth/register", registerRequest);
        var authData = await regResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        
        Authenticate(authData!.User.Id, authData.User.Email, authData.User.Role, authData.User.Name);

        // Act
        var response = await Client.GetAsync("/api/Auth/profile");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserDto>();
        profile.ShouldNotBeNull();
        profile.Email.ShouldBe(registerRequest.Email);
    }
}
