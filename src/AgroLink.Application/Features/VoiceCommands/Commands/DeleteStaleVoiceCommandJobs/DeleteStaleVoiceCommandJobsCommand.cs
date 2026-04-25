using AgroLink.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AgroLink.Application.Features.VoiceCommands.Commands.DeleteStaleVoiceCommandJobs;

public record DeleteStaleVoiceCommandJobsCommand(int OlderThanDays = 7) : IRequest<int>;

public class DeleteStaleVoiceCommandJobsCommandHandler(
    IVoiceCommandJobRepository jobRepository,
    ILogger<DeleteStaleVoiceCommandJobsCommandHandler> logger
) : IRequestHandler<DeleteStaleVoiceCommandJobsCommand, int>
{
    public async Task<int> Handle(
        DeleteStaleVoiceCommandJobsCommand request,
        CancellationToken cancellationToken
    )
    {
        var cutoff = DateTime.UtcNow.AddDays(-request.OlderThanDays);
        var deleted = await jobRepository.DeleteOlderThanAsync(cutoff, cancellationToken);

        logger.LogInformation(
            "Deleted {Count} stale VoiceCommandJob rows older than {Cutoff:O}",
            deleted,
            cutoff
        );

        return deleted;
    }
}
