using AgroLink.Application.Features.Checklists.Commands.Create;
using AgroLink.Application.Features.Checklists.Commands.Delete;
using AgroLink.Application.Features.Checklists.Commands.Update;
using AgroLink.Application.Features.Checklists.DTOs;
using AgroLink.Application.Features.Checklists.Queries.GetById;
using AgroLink.Application.Features.Checklists.Queries.GetByScope;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Route("api/farms/{farmId}/checklists")]
[Authorize(Policy = "FarmViewerAccess")]
public class ChecklistsController(IMediator mediator) : BaseController
{
    // Removed generic GetAll as it's not useful in farm context without filtering

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

    [HttpGet("scope/{scopeType}/{scopeId}")]
    public async Task<ActionResult<IEnumerable<ChecklistDto>>> GetByScope(
        int farmId,
        string scopeType,
        int scopeId
    )
    {
        var checklists = await mediator.Send(new GetChecklistsByScopeQuery(scopeType, scopeId));
        return Ok(checklists);
    }

    [HttpPost]
    [Authorize(Policy = "FarmEditorAccess")]
    public async Task<ActionResult<ChecklistDto>> Create(
        int farmId,
        [FromBody] CreateChecklistDto dto
    )
    {
        try
        {
            var userId = GetCurrentUserId();
            var checklist = await mediator.Send(new CreateChecklistCommand(dto, userId));
            return CreatedAtAction(nameof(GetById), new { farmId, id = checklist.Id }, checklist);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "FarmEditorAccess")]
    public async Task<ActionResult<ChecklistDto>> Update(
        int farmId,
        int id,
        [FromBody] CreateChecklistDto dto
    )
    {
        try
        {
            var checklist = await mediator.Send(new UpdateChecklistCommand(id, dto));
            return Ok(checklist);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "FarmEditorAccess")]
    public async Task<ActionResult> Delete(int farmId, int id)
    {
        try
        {
            await mediator.Send(new DeleteChecklistCommand(id));
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
