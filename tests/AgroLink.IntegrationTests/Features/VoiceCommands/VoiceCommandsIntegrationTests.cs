using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.VoiceCommands;

public class VoiceCommandsIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

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

    [Test]
    public async Task Submit_WhenValid_Returns202WithJobIdAndCreatesDbRecord()
    {
        var (user, farm) = await CreateUserAndFarmAsync();
        Authenticate(user);

        var content = BuildAudioContent(2048);
        var response = await Client.PostAsync($"/api/farms/{farm.Id}/voice/commands", content);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var jobId = body.GetProperty("jobId").GetGuid();
        jobId.ShouldNotBe(Guid.Empty);

        var job = DbContext.VoiceCommandJobs.SingleOrDefault(j => j.Id == jobId);
        job.ShouldNotBeNull();
        job!.FarmId.ShouldBe(farm.Id);
        job.UserId.ShouldBe(user.Id);
        job.Status.ShouldBe("pending");
    }

    // ── GET /api/voice/commands/{jobId} ──────────────────────────────────────

    [Test]
    public async Task GetStatus_WhenUnauthenticated_Returns401()
    {
        var response = await Client.GetAsync($"/api/voice/commands/{Guid.NewGuid()}");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetStatus_WhenJobNotFound_Returns404()
    {
        Authenticate(1, "user@test.com", "USER", "Test User");
        var response = await Client.GetAsync($"/api/voice/commands/{Guid.NewGuid()}");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetStatus_WhenJobBelongsToDifferentUser_Returns403()
    {
        var (ownerUser, farm) = await CreateUserAndFarmAsync();
        var otherUser = await CreateUserAsync("other@test.com");
        var job = await CreateJobAsync(farm.Id, ownerUser.Id, "pending");

        Authenticate(otherUser);
        var response = await Client.GetAsync($"/api/voice/commands/{job.Id}");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetStatus_PendingJob_ReturnsPendingStatus()
    {
        var (user, farm) = await CreateUserAndFarmAsync();
        var job = await CreateJobAsync(farm.Id, user.Id, "pending");

        Authenticate(user);
        var response = await Client.GetAsync($"/api/voice/commands/{job.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("status").GetString().ShouldBe("pending");
        body.TryGetProperty("result", out _).ShouldBeFalse();
    }

    [Test]
    public async Task GetStatus_CompletedJob_ReturnsResult()
    {
        var (user, farm) = await CreateUserAndFarmAsync();
        const string resultJson =
            """{"Intent":"move_animal","Confidence":0.91,"Entities":{},"RawTranscription":"mover negrita al lote norte"}""";
        var job = await CreateJobAsync(farm.Id, user.Id, "completed", resultJson);

        Authenticate(user);
        var response = await Client.GetAsync($"/api/voice/commands/{job.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("status").GetString().ShouldBe("completed");
        body.GetProperty("result").GetProperty("intent").GetString().ShouldBe("move_animal");
        body.GetProperty("result").GetProperty("confidence").GetDouble().ShouldBe(0.91);
    }

    [Test]
    public async Task GetStatus_FailedJob_ReturnsError()
    {
        var (user, farm) = await CreateUserAndFarmAsync();
        var job = await CreateJobAsync(
            farm.Id,
            user.Id,
            "failed",
            error: "Transcription service unavailable."
        );

        Authenticate(user);
        var response = await Client.GetAsync($"/api/voice/commands/{job.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("status").GetString().ShouldBe("failed");
        body.GetProperty("error").GetString().ShouldBe("Transcription service unavailable.");
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
                new FarmMember
                {
                    FarmId = farm.Id,
                    UserId = user.Id,
                    Role = FarmMemberRoles.Editor,
                }
            );
            await DbContext.SaveChangesAsync();
        }

        return (user, farm);
    }

    private async Task<User> CreateUserAsync(string email = "user@test.com")
    {
        var user = new User
        {
            Name = "Test User",
            Email = email,
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();
        return user;
    }

    private async Task<VoiceCommandJob> CreateJobAsync(
        int farmId,
        int userId,
        string status,
        string? resultJson = null,
        string? error = null
    )
    {
        var job = new VoiceCommandJob
        {
            Id = Guid.NewGuid(),
            FarmId = farmId,
            UserId = userId,
            S3Key = $"voice-commands/temp/{Guid.NewGuid()}",
            Status = status,
            ResultJson = resultJson,
            ErrorMessage = error,
            CreatedAt = DateTime.UtcNow,
        };
        DbContext.VoiceCommandJobs.Add(job);
        await DbContext.SaveChangesAsync();
        return job;
    }
}
