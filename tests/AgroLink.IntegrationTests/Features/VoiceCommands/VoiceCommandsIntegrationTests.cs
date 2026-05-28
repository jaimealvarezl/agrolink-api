using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.VoiceCommands;

public class VoiceCommandsIntegrationTests : IntegrationTestBase
{
    // ── POST /api/farms/{farmId}/voice/commands ──────────────────────────────

    [Test]
    public async Task Submit_WhenUnauthenticated_Returns401()
    {
        var content = BuildAudioContent(2048);
        var response = await Client.PostAsync("/api/farms/1/voice/commands", content);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Submit_WhenNotFarmMember_Returns403()
    {
        var (user, farm) = await CreateUserAndFarmAsync(false);
        Authenticate(user);

        var content = BuildAudioContent(2048);
        var response = await Client.PostAsync($"/api/farms/{farm.Id}/voice/commands", content);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Submit_WhenUnsupportedContentType_Returns415()
    {
        var (user, farm) = await CreateUserAndFarmAsync();
        Authenticate(user);

        var content = BuildAudioContent(2048, "video/mp4");
        var response = await Client.PostAsync($"/api/farms/{farm.Id}/voice/commands", content);
        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
    }

    [Test]
    public async Task Submit_WhenAudioTooSmall_Returns400()
    {
        var (user, farm) = await CreateUserAndFarmAsync();
        Authenticate(user);

        var content = BuildAudioContent(512); // < 1 KB
        var response = await Client.PostAsync($"/api/farms/{farm.Id}/voice/commands", content);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Submit_WhenAudioTooLarge_Returns413()
    {
        var (user, farm) = await CreateUserAndFarmAsync();
        Authenticate(user);

        var content = BuildAudioContent(10 * 1024 * 1024 + 1); // > 10 MB
        var response = await Client.PostAsync($"/api/farms/{farm.Id}/voice/commands", content);
        response.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static MultipartFormDataContent BuildAudioContent(
        int size,
        string contentType = "audio/m4a"
    )
    {
        var bytes = new byte[size];
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var form = new MultipartFormDataContent();
        form.Add(fileContent, "audio", "command.m4a");
        return form;
    }

    private async Task<(User user, Farm farm)> CreateUserAndFarmAsync(bool addMember = true)
    {
        var user = await CreateUserAsync();
        var owner = new Owner { Name = "Owner", Phone = "123" };
        DbContext.Owners.Add(owner);
        await DbContext.SaveChangesAsync();

        var farm = new Farm { Name = "Test Farm", OwnerId = owner.Id };
        DbContext.Farms.Add(farm);
        await DbContext.SaveChangesAsync();

        if (addMember)
        {
            DbContext.FarmMembers.Add(
                new FarmMember { FarmId = farm.Id, UserId = user.Id, Role = FarmMemberRoles.Editor, }
            );
            await DbContext.SaveChangesAsync();
        }

        return (user, farm);
    }

    private async Task<User> CreateUserAsync(string email = "user@test.com")
    {
        var user = new User
        {
            Name = "Test User", Email = email, PasswordHash = "hash", Role = "USER",
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();
        return user;
    }
}
