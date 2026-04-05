using AgroLink.Application.Features.ClinicalCases.DTOs;
using AgroLink.Application.Features.ClinicalCases.Queries.GetById;
using AgroLink.Application.Features.ClinicalCases.Queries.GetLatestReport;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[ApiController]
[Route("api/clinical")]
public class ClinicalCasesController(IMediator mediator) : ControllerBase
{
    [HttpGet("animals/{earTag}/latest-report")]
    public async Task<ActionResult<ClinicalLatestReportDto>> GetLatestReport(
        string earTag,
        [FromQuery] string farmReference,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(farmReference))
        {
            return BadRequest("farmReference is required.");
        }

        var report = await mediator.Send(
            new GetLatestAnimalClinicalReportQuery(farmReference, earTag),
            cancellationToken
        );

        if (report == null)
        {
            return NotFound();
        }

        return Ok(report);
    }

    [HttpGet("cases/{caseId:int}")]
    public async Task<ActionResult<ClinicalCaseDetailDto>> GetCaseById(
        int caseId,
        CancellationToken cancellationToken
    )
    {
        var clinicalCase = await mediator.Send(
            new GetClinicalCaseByIdQuery(caseId),
            cancellationToken
        );
        if (clinicalCase == null)
        {
            return NotFound();
        }

        return Ok(clinicalCase);
    }
}
