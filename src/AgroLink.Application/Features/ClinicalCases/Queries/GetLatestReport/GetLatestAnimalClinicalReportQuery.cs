using AgroLink.Application.Features.ClinicalCases.DTOs;
using MediatR;

namespace AgroLink.Application.Features.ClinicalCases.Queries.GetLatestReport;

public record GetLatestAnimalClinicalReportQuery(string FarmReference, string EarTag)
    : IRequest<ClinicalLatestReportDto?>;
