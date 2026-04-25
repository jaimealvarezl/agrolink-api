using System.Text.Json;
using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.VoiceCommands.DTOs;
using AgroLink.Application.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.VoiceCommands.Queries.GetVoiceCommandJob;

public record GetVoiceCommandJobQuery(Guid JobId, int RequestingUserId)
    : IRequest<VoiceCommandJobStatusDto>;

public class GetVoiceCommandJobQueryHandler(IVoiceCommandJobRepository jobRepository)
    : IRequestHandler<GetVoiceCommandJobQuery, VoiceCommandJobStatusDto>
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
        var job = await jobRepository.GetByIdAsync(request.JobId, cancellationToken);

        if (job == null)
        {
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

        return new VoiceCommandJobStatusDto(job.Status, result, job.ErrorMessage);
    }
}
