using System.Diagnostics;
using System.Text.Json;
using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.VoiceCommands.DTOs;
using AgroLink.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AgroLink.Application.Features.VoiceCommands.Queries.GetVoiceCommandJob;

public record GetVoiceCommandJobQuery(Guid JobId, int RequestingUserId)
    : IRequest<VoiceCommandJobStatusDto>;

public class GetVoiceCommandJobQueryHandler(
    IVoiceCommandJobRepository jobRepository,
    ILogger<GetVoiceCommandJobQueryHandler> logger
) : IRequestHandler<GetVoiceCommandJobQuery, VoiceCommandJobStatusDto>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<VoiceCommandJobStatusDto> Handle(
        GetVoiceCommandJobQuery request,
        CancellationToken cancellationToken
    )
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("[voice-poll] START job={JobId}", request.JobId);

        var job = await jobRepository.GetByIdAsync(request.JobId, cancellationToken);
        logger.LogInformation(
            "[voice-poll] DB query done in {Ms}ms job={JobId}",
            sw.ElapsedMilliseconds,
            request.JobId
        );

        if (job == null)
        {
            logger.LogWarning("[voice-poll] NOT FOUND job={JobId}", request.JobId);
            throw new NotFoundException($"Voice command job {request.JobId} not found.");
        }

        if (job.UserId != request.RequestingUserId)
        {
            throw new ForbiddenAccessException();
        }

        VoiceCommandResultDto? result = null;
        if (job is { Status: "completed", ResultJson: not null })
        {
            result = JsonSerializer.Deserialize<VoiceCommandResultDto>(job.ResultJson, JsonOptions);
        }

        logger.LogInformation(
            "[voice-poll] DONE status={Status} total={Ms}ms job={JobId}",
            job.Status,
            sw.ElapsedMilliseconds,
            request.JobId
        );

        return new VoiceCommandJobStatusDto(job.Status, result, job.ErrorMessage);
    }
}
