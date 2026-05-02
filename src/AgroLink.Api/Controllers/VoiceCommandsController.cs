using AgroLink.Application.Features.VoiceCommands.Commands.ProcessVoiceCommandInline;
using AgroLink.Application.Features.VoiceCommands.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

public class VoiceCommandsController(IMediator mediator) : BaseController
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
    public async Task<ActionResult<VoiceCommandResultDto>> Submit(
        int farmId,
        IFormFile? audio,
        CancellationToken cancellationToken
    )
    {
        if (audio == null)
        {
            return BadRequest("Audio file is required.");
        }

        if (
            string.IsNullOrEmpty(audio.ContentType)
            || !AllowedAudioTypes.Contains(audio.ContentType.ToLowerInvariant())
        )
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

        await using var stream = audio.OpenReadStream();

        var result = await mediator.Send(
            new ProcessVoiceCommandInlineCommand(
                farmId,
                userId,
                stream,
                audio.ContentType,
                audio.Length
            ),
            cancellationToken
        );

        return Ok(result);
    }
}
