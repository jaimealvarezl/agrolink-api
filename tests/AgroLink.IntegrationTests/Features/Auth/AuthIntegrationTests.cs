using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using AgroLink.Application.Features.Auth.DTOs;
using AgroLink.Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.Auth;

public class AuthIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetProfile_WithValidFirebaseToken_ShouldReturnUser()
    {
        var user = new User
        {
            Name = "Profile User",
            Email = "profile@example.com",
            FirebaseUid = "firebase-uid-profile",
            Role = "USER",
            IsActive = true,
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        var response = await Client.GetAsync("/api/Auth/profile");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserDto>();
        profile.ShouldNotBeNull();
        profile.Email.ShouldBe(user.Email);
        profile.Name.ShouldBe(user.Name);
    }

    [Test]
    public async Task GetProfile_WithoutToken_ShouldReturnUnauthorized()
    {
        var response = await Client.GetAsync("/api/Auth/profile");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetProfile_NewFirebaseUser_AutoProvisionAndReturnProfile()
    {
        // No user in DB — FirebaseUserMiddleware creates one on first authenticated request
        const string firebaseUid = "new-firebase-uid-auto";
        const string email = "auto@example.com";
        const string name = "Auto Provisioned";

        var token = CreateTestToken(firebaseUid, email, name);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await Client.GetAsync("/api/Auth/profile");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserDto>();
        profile.ShouldNotBeNull();
        profile.Email.ShouldBe(email);
        profile.Name.ShouldBe(name);
    }

    private static string CreateTestToken(string firebaseUid, string email, string name)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(CustomWebApplicationFactory<Program>.TestJwtKey)
        );
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim("sub", firebaseUid),
                new Claim("email", email),
                new Claim("name", name),
            ]),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256Signature
            ),
        };
        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }
}
