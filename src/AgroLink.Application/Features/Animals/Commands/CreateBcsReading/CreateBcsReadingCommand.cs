using System.Globalization;
using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Commands.CreateBcsReading;

public record CreateBcsReadingCommand(int FarmId, int AnimalId, int UserId, CreateBcsReadingDto Dto)
    : IRequest<AnimalBcsReadingDto>;

public class CreateBcsReadingCommandHandler(
    IAnimalRepository animalRepository,
    IAnimalBcsReadingRepository bcsReadingRepository,
    IAnimalNoteRepository animalNoteRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateBcsReadingCommand, AnimalBcsReadingDto>
{
    public async Task<AnimalBcsReadingDto> Handle(
        CreateBcsReadingCommand request,
        CancellationToken cancellationToken
    )
    {
        var dto = request.Dto;

        if (dto.Score < 1.0 || dto.Score > 5.0)
        {
            throw new ArgumentException("Score must be between 1.0 and 5.0.");
        }

        _ =
            await animalRepository.GetByIdInFarmAsync(
                request.AnimalId,
                request.FarmId,
                cancellationToken
            ) ?? throw new NotFoundException("Animal", request.AnimalId);

        var reading = new AnimalBcsReading
        {
            AnimalId = request.AnimalId,
            Score = dto.Score,
            Source = dto.Source,
            ConfirmedByUserId = request.UserId,
            RawAiResponse = dto.RawAiResponse,
            CreatedAt = DateTime.UtcNow,
        };

        await bcsReadingRepository.AddAsync(reading, cancellationToken);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        var userName = user?.Name ?? string.Empty;
        var scoreText = dto.Score.ToString("0.0#", CultureInfo.InvariantCulture);

        var sourceText = dto.Source == BcsReadingSource.AI ? "Análisis IA" : "Lectura manual";
        var bcsNote = new AnimalNote
        {
            AnimalId = request.AnimalId,
            Content = $"CC {scoreText} — {sourceText} confirmado por {userName}",
            UserId = request.UserId,
            CreatedAt = DateTime.UtcNow,
        };
        await animalNoteRepository.AddAsync(bcsNote, cancellationToken);

        if (dto.HasAlerts && !string.IsNullOrWhiteSpace(dto.AlertDescription))
        {
            var alertNote = new AnimalNote
            {
                AnimalId = request.AnimalId,
                Content = $"Alerta IA: {dto.AlertDescription}",
                UserId = request.UserId,
                CreatedAt = DateTime.UtcNow,
            };
            await animalNoteRepository.AddAsync(alertNote, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AnimalBcsReadingDto
        {
            Id = reading.Id,
            AnimalId = reading.AnimalId,
            Score = reading.Score,
            Source = reading.Source,
            ConfirmedByUserId = reading.ConfirmedByUserId,
            CreatedAt = reading.CreatedAt,
        };
    }
}
