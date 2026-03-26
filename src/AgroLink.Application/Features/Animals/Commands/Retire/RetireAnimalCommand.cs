using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Commands.Retire;

public record RetireAnimalCommand(
    int FarmId,
    int AnimalId,
    int UserId,
    RetirementReason Reason,
    DateTime At,
    decimal? SalePrice,
    string? Notes
) : IRequest<AnimalRetirementDto>;

public class RetireAnimalCommandHandler(
    IAnimalRepository animalRepository,
    IAnimalRetirementRepository animalRetirementRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<RetireAnimalCommand, AnimalRetirementDto>
{
    public async Task<AnimalRetirementDto> Handle(
        RetireAnimalCommand request,
        CancellationToken cancellationToken
    )
    {
        var animal =
            await animalRepository.GetByIdInFarmAsync(request.AnimalId, request.FarmId)
            ?? throw new NotFoundException("Animal", request.AnimalId);

        if (animal.LifeStatus != LifeStatus.Active)
        {
            throw new ConflictException("Animal is already retired.");
        }

        animal.LifeStatus = request.Reason switch
        {
            RetirementReason.Sold => LifeStatus.Sold,
            RetirementReason.Dead => LifeStatus.Dead,
            RetirementReason.Stolen => LifeStatus.Missing,
            RetirementReason.Other => LifeStatus.Retired,
            _ => LifeStatus.Retired,
        };
        var now = DateTime.UtcNow;
        animal.UpdatedAt = now;

        var retirement = new AnimalRetirement
        {
            AnimalId = request.AnimalId,
            UserId = request.UserId,
            Reason = request.Reason,
            At = request.At,
            SalePrice = request.SalePrice,
            Notes = request.Notes,
            CreatedAt = now,
        };

        await animalRetirementRepository.AddAsync(retirement);
        await unitOfWork.SaveChangesAsync();

        retirement.User = await userRepository.GetByIdAsync(request.UserId) ?? retirement.User;

        return AnimalRetirementDto.From(retirement);
    }
}
