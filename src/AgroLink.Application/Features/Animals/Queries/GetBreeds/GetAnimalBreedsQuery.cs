using MediatR;

namespace AgroLink.Application.Features.Animals.Queries.GetBreeds;

public record GetAnimalBreedsQuery(int FarmId) : IRequest<List<string>>;
