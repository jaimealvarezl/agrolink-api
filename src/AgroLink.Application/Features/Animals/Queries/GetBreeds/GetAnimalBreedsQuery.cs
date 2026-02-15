using MediatR;

namespace AgroLink.Application.Features.Animals.Queries.GetBreeds;

public record GetAnimalBreedsQuery(int UserId) : IRequest<List<string>>;
