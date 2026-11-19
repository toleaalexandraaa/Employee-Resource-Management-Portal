using company_resource_manager.Server.DTOs.Crm;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace company_resource_manager.Server.Controllers;

[ApiController]
[Route("api/leads")]
[Authorize(Roles = "Admin,AccountManager")]
public class LeadsController : ControllerBase
{
    private readonly ILeadService _leads;

    public LeadsController(ILeadService leads) => _leads = leads;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _leads.GetAllAsync(page, pageSize));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dto = await _leads.GetByIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeadRequest request)
    {
        var dto = await _leads.CreateAsync(request);
        return Created(string.Empty, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeadRequest request)
    {
        var dto = await _leads.UpdateAsync(id, request);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("{id:guid}/advance-stage")]
    public async Task<IActionResult> AdvanceStage(Guid id)
    {
        var dto = await _leads.AdvanceStageAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("{id:guid}/convert")]
    public async Task<IActionResult> Convert(Guid id, [FromBody] ConvertLeadRequest request)
    {
        try
        {
            var client = await _leads.ConvertToClientAsync(id, request);
            return Created(string.Empty, client);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,AccountManager")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _leads.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
