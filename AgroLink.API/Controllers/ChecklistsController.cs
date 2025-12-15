using AgroLink.Application.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.API.Controllers;

[Route("api/[controller]")]
public class ChecklistsController(IChecklistService checklistService) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChecklistDto>>> GetAll()
    {
        var checklists = await checklistService.GetAllAsync();
        return Ok(checklists);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ChecklistDto>> GetById(int id)
    {
        var checklist = await checklistService.GetByIdAsync(id);
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
        var checklists = await checklistService.GetByScopeAsync(scopeType, scopeId);
        return Ok(checklists);
    }

    [HttpPost]
    public async Task<ActionResult<ChecklistDto>> Create(CreateChecklistDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var checklist = await checklistService.CreateAsync(dto, userId);
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
            var checklist = await checklistService.UpdateAsync(id, dto);
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
            await checklistService.DeleteAsync(id);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
