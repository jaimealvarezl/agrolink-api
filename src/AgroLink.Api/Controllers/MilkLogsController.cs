using System.Globalization;
using AgroLink.Application.Features.MilkLogs.Commands.UpsertMilkLog;
using AgroLink.Application.Features.MilkLogs.DTOs;
using AgroLink.Application.Features.MilkLogs.Queries.GetMilkLogByDate;
using AgroLink.Application.Features.MilkLogs.Queries.GetMilkLogs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Route("api/farms/{farmId}/milk-logs")]
[Authorize(Policy = "FarmViewerAccess")]
public class MilkLogsController(IMediator mediator) : BaseController
{
    [HttpPost]
    [Authorize(Policy = "FarmEditorAccess")]
    public async Task<ActionResult<MilkLogDto>> Upsert(
        int farmId,
        [FromBody] UpsertMilkLogRequest request,
        CancellationToken cancellationToken
    )
    {
        var userId = GetCurrentUserId();
        var result = await mediator.Send(
            new UpsertMilkLogCommand(farmId, userId, request),
            cancellationToken
        );

        return result.IsNew
            ? CreatedAtAction(
                nameof(GetByDate),
                new
                {
                    farmId,
                    date = result.Log.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                },
                result.Log
            )
            : Ok(result.Log);
    }

    [HttpGet]
    public async Task<ActionResult<MilkLogsListDto>> GetList(
        int farmId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken cancellationToken = default
    )
    {
        var result = await mediator.Send(
            new GetMilkLogsQuery(farmId, from, to, page, pageSize),
            cancellationToken
        );
        return Ok(result);
    }

    [HttpGet("{date}")]
    public async Task<ActionResult<MilkLogDto>> GetByDate(
        int farmId,
        DateOnly date,
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(
            new GetMilkLogByDateQuery(farmId, date),
            cancellationToken
        );

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }
}
