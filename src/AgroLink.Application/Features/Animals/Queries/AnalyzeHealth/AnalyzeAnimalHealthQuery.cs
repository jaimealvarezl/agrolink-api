using AgroLink.Application.Features.Animals.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Animals.Queries.AnalyzeHealth;

public record AnalyzeAnimalHealthQuery(int AnimalId, int FarmId, int UserId)
    : IRequest<AnimalHealthAnalysisDto>;
