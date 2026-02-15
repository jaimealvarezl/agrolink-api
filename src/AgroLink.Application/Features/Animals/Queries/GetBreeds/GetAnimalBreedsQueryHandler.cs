using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Queries.GetBreeds;

public class GetAnimalBreedsQueryHandler(IAnimalRepository animalRepository)
    : IRequestHandler<GetAnimalBreedsQuery, List<string>>
{
    public async Task<List<string>> Handle(
        GetAnimalBreedsQuery request,
        CancellationToken cancellationToken
    )
    {
        return await animalRepository.GetDistinctBreedsAsync(request.UserId, cancellationToken);
    }
}
