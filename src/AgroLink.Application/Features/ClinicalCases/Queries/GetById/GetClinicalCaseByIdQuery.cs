using AgroLink.Application.Features.ClinicalCases.DTOs;
using MediatR;

namespace AgroLink.Application.Features.ClinicalCases.Queries.GetById;

public record GetClinicalCaseByIdQuery(int CaseId) : IRequest<ClinicalCaseDetailDto?>;
