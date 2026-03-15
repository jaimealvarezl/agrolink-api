using AgroLink.Application.Common.Utilities;
using AgroLink.Application.Features.Checklists.Commands.Create;
using AgroLink.Application.Features.Checklists.Commands.Delete;
using AgroLink.Application.Features.Checklists.Commands.Update;
using AgroLink.Application.Features.Checklists.DTOs;
using AgroLink.Application.Features.Checklists.Queries.GetByFarm;
using AgroLink.Application.Features.Checklists.Queries.GetById;
using AgroLink.Application.Features.Checklists.Queries.GetByLot;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Route("api/farms/{farmId}/checklists")]
[Authorize(Policy = "FarmViewerAccess")]
public class ChecklistsController(IMediator mediator) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ChecklistDto>>> GetByFarm(
        int farmId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20
    )
    {
        var result = await mediator.Send(new GetChecklistsByFarmQuery(farmId, page, pageSize));
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ChecklistDto>> GetById(int farmId, int id)
    {
        var checklist = await mediator.Send(new GetChecklistByIdQuery(id));
        if (checklist == null)
        {
            return NotFound();
        }

        return Ok(checklist);
    }

    [HttpGet("lot/{lotId}")]
    public async Task<ActionResult<IEnumerable<ChecklistDto>>> GetByLot(int farmId, int lotId)
    {
        var checklists = await mediator.Send(new GetChecklistsByLotQuery(lotId));
        return Ok(checklists);
    }

    [HttpPost]
    [Authorize(Policy = "FarmEditorAccess")]
    public async Task<ActionResult<ChecklistDto>> Create(
        int farmId,
        [FromBody] CreateChecklistDto dto
    )
    {
        var userId = GetCurrentUserId();
        var checklist = await mediator.Send(new CreateChecklistCommand(dto, userId));
        return CreatedAtAction(nameof(GetById), new { farmId, id = checklist.Id }, checklist);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "FarmEditorAccess")]
    public async Task<ActionResult<ChecklistDto>> Update(
        int farmId,
        int id,
        [FromBody] CreateChecklistDto dto
    )
    {
        var checklist = await mediator.Send(new UpdateChecklistCommand(id, dto));
        return Ok(checklist);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "FarmEditorAccess")]
    public async Task<ActionResult> Delete(int farmId, int id)
    {
        await mediator.Send(new DeleteChecklistCommand(id));
        return NoContent();
    }
}
