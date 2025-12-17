using AgroLink.Application.DTOs;
using AgroLink.Application.Features.Checklists.Commands.Create;
using AgroLink.Application.Features.Checklists.Commands.Delete;
using AgroLink.Application.Features.Checklists.Commands.Update;
using AgroLink.Application.Features.Checklists.Queries.GetAll;
using AgroLink.Application.Features.Checklists.Queries.GetById;
using AgroLink.Application.Features.Checklists.Queries.GetByScope;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.Api.Controllers;

[Route("api/[controller]")]
public class ChecklistsController(IMediator mediator) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChecklistDto>>> GetAll()
    {
        var checklists = await mediator.Send(new GetAllChecklistsQuery());
        return Ok(checklists);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ChecklistDto>> GetById(int id)
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
        string scopeType,
        int scopeId
    )
    {
        var checklists = await mediator.Send(new GetChecklistsByScopeQuery(scopeType, scopeId));
        return Ok(checklists);
    }

    [HttpPost]
    public async Task<ActionResult<ChecklistDto>> Create(CreateChecklistDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var checklist = await mediator.Send(new CreateChecklistCommand(dto, userId));
            return CreatedAtAction(nameof(GetById), new { id = checklist.Id }, checklist);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ChecklistDto>> Update(int id, CreateChecklistDto dto)
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
    public async Task<ActionResult> Delete(int id)
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
