using MediatR;

namespace AgroLink.Application.Features.Animals.Queries.GetColors;

public record GetAnimalColorsQuery(int UserId) : IRequest<List<string>>;
