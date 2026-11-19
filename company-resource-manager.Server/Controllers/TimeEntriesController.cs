using System.Security.Claims;
using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Proj;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Server.Controllers;

[ApiController]
[Route("api/time-entries")]
[Authorize]
public class TimeEntriesController : ControllerBase
{
    private readonly ITimeEntryService _timeEntries;
    private readonly AppDbContext _db;

    public TimeEntriesController(ITimeEntryService timeEntries, AppDbContext db)
    {
        _timeEntries = timeEntries;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? employeeId,
        [FromQuery] Guid? projectId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        // Employees only see their own entries
        if (User.IsInRole("Employee") && !User.IsInRole("Admin") && !User.IsInRole("ProjectManager"))
        {
            var eid = await GetCurrentEmployeeIdAsync();
            employeeId = eid;
        }
        return Ok(await _timeEntries.GetAllAsync(employeeId, projectId, from, to, page, pageSize));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTimeEntryRequest request)
    {
        var employeeId = await GetCurrentEmployeeIdAsync();
        if (employeeId is null)
            return BadRequest(new { message = "No employee profile linked to this user." });

        try
        {
            var dto = await _timeEntries.CreateAsync(request, employeeId.Value);
            return Created(string.Empty, dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTimeEntryRequest request)
    {
        var employeeId = await GetCurrentEmployeeIdAsync();
        if (employeeId is null) return Forbid();

        var dto = await _timeEntries.UpdateAsync(id, request, employeeId.Value);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var employeeId = await GetCurrentEmployeeIdAsync();
        if (employeeId is null) return Forbid();

        var deleted = await _timeEntries.DeleteAsync(id, employeeId.Value);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("summary")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> GetSummary([FromQuery] Guid projectId)
    {
        var total = await _timeEntries.GetProjectTotalHoursAsync(projectId);
        return Ok(new { projectId, totalHours = total });
    }

    private async Task<Guid?> GetCurrentEmployeeIdAsync()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        return user?.EmployeeId;
    }
}
