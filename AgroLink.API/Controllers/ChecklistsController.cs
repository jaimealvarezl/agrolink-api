using AgroLink.Core.DTOs;
using AgroLink.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChecklistsController : ControllerBase
{
    private readonly IChecklistService _checklistService;

    public ChecklistsController(IChecklistService checklistService)
    {
        _checklistService = checklistService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChecklistDto>>> GetAll()
    {
        var checklists = await _checklistService.GetAllAsync();
        return Ok(checklists);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ChecklistDto>> GetById(int id)
    {
        var checklist = await _checklistService.GetByIdAsync(id);
        if (checklist == null)
            return NotFound();

        return Ok(checklist);
    }

    [HttpGet("scope/{scopeType}/{scopeId}")]
    public async Task<ActionResult<IEnumerable<ChecklistDto>>> GetByScope(
        string scopeType,
        int scopeId
    )
    {
        var checklists = await _checklistService.GetByScopeAsync(scopeType, scopeId);
        return Ok(checklists);
    }

    [HttpPost]
    public async Task<ActionResult<ChecklistDto>> Create(CreateChecklistDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var checklist = await _checklistService.CreateAsync(dto, userId);
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
            var checklist = await _checklistService.UpdateAsync(id, dto);
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
            await _checklistService.DeleteAsync(id);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userid");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            throw new UnauthorizedAccessException("Invalid user token");
        return userId;
    }
}
