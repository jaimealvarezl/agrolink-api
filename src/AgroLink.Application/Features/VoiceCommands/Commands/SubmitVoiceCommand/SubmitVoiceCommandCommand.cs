using System.Diagnostics;
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
    IVoiceCommandQueue queue,
    IUnitOfWork unitOfWork,
    ILogger<SubmitVoiceCommandCommandHandler> logger
) : IRequestHandler<SubmitVoiceCommandCommand, Guid>
{
    public async Task<Guid> Handle(
        SubmitVoiceCommandCommand request,
        CancellationToken cancellationToken
    )
    {
        var sw = Stopwatch.StartNew();
        var jobId = Guid.NewGuid();
        var s3Key = pathProvider.GetVoiceAudioPath(jobId);

        logger.LogInformation(
            "[voice-submit] START job={JobId} farm={FarmId} user={UserId} size={SizeBytes}B contentType={ContentType}",
            jobId,
            request.FarmId,
            request.UserId,
            request.Size,
            request.ContentType
        );

        await storageService.UploadFileAsync(
            s3Key,
            request.AudioStream,
            request.ContentType,
            request.Size
        );
        logger.LogInformation("[voice-submit] S3 upload done in {ElapsedMs}ms job={JobId}", sw.ElapsedMilliseconds, jobId);

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
        logger.LogInformation("[voice-submit] DB saved in {ElapsedMs}ms job={JobId}", sw.ElapsedMilliseconds, jobId);

        await queue.EnqueueAsync(jobId, request.FarmId, request.UserId, cancellationToken);
        logger.LogInformation("[voice-submit] SQS enqueued in {ElapsedMs}ms job={JobId}", sw.ElapsedMilliseconds, jobId);

        logger.LogInformation("[voice-submit] DONE total={ElapsedMs}ms job={JobId}", sw.ElapsedMilliseconds, jobId);

        return jobId;
    }
}
