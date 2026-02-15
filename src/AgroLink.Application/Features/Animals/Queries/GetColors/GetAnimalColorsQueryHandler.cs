using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Queries.GetColors;

public class GetAnimalColorsQueryHandler(IAnimalRepository animalRepository)
    : IRequestHandler<GetAnimalColorsQuery, List<string>>
{
    public async Task<List<string>> Handle(
        GetAnimalColorsQuery request,
        CancellationToken cancellationToken
    )
    {
        return await animalRepository.GetDistinctColorsAsync(request.UserId, cancellationToken);
    }
}
