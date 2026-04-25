using System.Text.Json;
using AgroLink.Application.Features.VoiceCommands.Commands.SubmitVoiceCommand;
using AgroLink.Application.Features.VoiceCommands.DTOs;
using AgroLink.Application.Features.VoiceCommands.Queries.GetVoiceCommandJob;
using Amazon.SQS;
using Amazon.SQS.Model;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

public class VoiceCommandsController(
    IMediator mediator,
    IAmazonSQS sqsClient,
    IConfiguration configuration
) : BaseController
{
    private const long MinAudioBytes = 1024;
    private const long MaxAudioBytes = 10 * 1024 * 1024;

    private static readonly string[] AllowedAudioTypes =
    [
        "audio/m4a",
        "audio/x-m4a",
        "audio/wav",
        "audio/wave",
        "audio/ogg",
        "audio/webm",
        "audio/mpeg",
        "audio/mp4",
    ];

    [HttpPost("api/farms/{farmId}/voice/commands")]
    [Authorize(Policy = "FarmEditorAccess")]
    public async Task<IActionResult> Submit(
        int farmId,
        IFormFile audio,
        CancellationToken cancellationToken
    )
    {
        if (!AllowedAudioTypes.Contains(audio.ContentType.ToLowerInvariant()))
        {
            return StatusCode(415, "Unsupported audio content type.");
        }

        if (audio.Length < MinAudioBytes)
        {
            return BadRequest("Audio file is too small (minimum 1 KB).");
        }

        if (audio.Length > MaxAudioBytes)
        {
            return StatusCode(413, "Audio file is too large (maximum 10 MB).");
        }

        var userId = GetCurrentUserId();

        var jobId = await mediator.Send(
            new SubmitVoiceCommandCommand(
                farmId,
                userId,
                audio.OpenReadStream(),
                audio.ContentType,
                audio.Length
            ),
            cancellationToken
        );

        await EnqueueJobAsync(jobId, farmId, userId, cancellationToken);

        return StatusCode(202, new { jobId });
    }

    [HttpGet("api/voice/commands/{jobId:guid}")]
    public async Task<ActionResult<VoiceCommandJobStatusDto>> GetStatus(
        Guid jobId,
        CancellationToken cancellationToken
    )
    {
        var userId = GetCurrentUserId();
        var result = await mediator.Send(
            new GetVoiceCommandJobQuery(jobId, userId),
            cancellationToken
        );
        return Ok(result);
    }

    private async Task EnqueueJobAsync(Guid jobId, int farmId, int userId, CancellationToken ct)
    {
        var queueUrl = configuration["VoiceCommands:SqsQueueUrl"];
        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            return;
        }

        var body = JsonSerializer.Serialize(
            new
            {
                jobId,
                farmId,
                userId,
            }
        );
        await sqsClient.SendMessageAsync(
            new SendMessageRequest { QueueUrl = queueUrl, MessageBody = body },
            ct
        );
    }
}
