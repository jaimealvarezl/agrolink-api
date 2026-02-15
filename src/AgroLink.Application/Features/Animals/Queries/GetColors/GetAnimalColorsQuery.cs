using MediatR;

namespace AgroLink.Application.Features.Animals.Queries.GetColors;

public record GetAnimalColorsQuery(string Query, int Limit = 10) : IRequest<List<string>>;
