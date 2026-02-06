using AgroLink.Application.Features.Animals.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Animals.Queries.GetDetail;

public record GetAnimalDetailQuery(int Id) : IRequest<AnimalDetailDto?>;
