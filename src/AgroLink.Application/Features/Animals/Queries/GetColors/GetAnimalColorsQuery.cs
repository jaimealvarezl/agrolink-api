using MediatR;

namespace AgroLink.Application.Features.Animals.Queries.GetColors;

public record GetAnimalColorsQuery(int FarmId) : IRequest<List<string>>;
