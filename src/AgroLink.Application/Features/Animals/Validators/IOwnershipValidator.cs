using AgroLink.Application.Features.Animals.DTOs;

namespace AgroLink.Application.Features.Animals.Validators;

public interface IOwnershipValidator
{
    Task ValidateAsync(List<AnimalOwnerCreateDto> owners, int targetFarmId);
}
