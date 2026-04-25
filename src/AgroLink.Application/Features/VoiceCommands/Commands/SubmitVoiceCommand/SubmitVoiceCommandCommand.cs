using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AgroLink.Application.Features.VoiceCommands.Commands.SubmitVoiceCommand;

public record SubmitVoiceCommandCommand(
    int FarmId,
    int UserId,
    Stream AudioStream,
    string ContentType,
    long Size
) : IRequest<Guid>;

public class SubmitVoiceCommandCommandHandler(
    IVoiceCommandJobRepository jobRepository,
    IStorageService storageService,
    IStoragePathProvider pathProvider,
    IUnitOfWork unitOfWork,
    ILogger<SubmitVoiceCommandCommandHandler> logger
) : IRequestHandler<SubmitVoiceCommandCommand, Guid>
{
    public async Task<Guid> Handle(
        SubmitVoiceCommandCommand request,
        CancellationToken cancellationToken
    )
    {
        var jobId = Guid.NewGuid();
        var s3Key = pathProvider.GetVoiceAudioPath(jobId);

        logger.LogInformation(
            "Submitting voice command job {JobId} for farm {FarmId}, user {UserId}",
            jobId,
            request.FarmId,
            request.UserId
        );

        await storageService.UploadFileAsync(
            s3Key,
            request.AudioStream,
            request.ContentType,
            request.Size
        );

        var job = new VoiceCommandJob
        {
            Id = jobId,
            FarmId = request.FarmId,
            UserId = request.UserId,
            S3Key = s3Key,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
        };

        await jobRepository.AddAsync(job, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Voice command job {JobId} created with S3 key {S3Key}",
            jobId,
            s3Key
        );

        return jobId;
    }
}
