using System.Security.Claims;
using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Hr;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Server.Controllers;

[ApiController]
[Route("api/leave-requests")]
[Authorize]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _leaveService;
    private readonly AppDbContext _db;

    public LeaveRequestsController(ILeaveRequestService leaveService, AppDbContext db)
    {
        _leaveService = leaveService;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? employeeId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        // Employees only see their own; HR/Admin see all
        if (User.IsInRole("Employee") && !User.IsInRole("HrManager") && !User.IsInRole("Admin"))
        {
            var eid = await GetCurrentEmployeeIdAsync();
            employeeId = eid;
        }
        return Ok(await _leaveService.GetAllAsync(employeeId, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dto = await _leaveService.GetByIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] CreateLeaveRequest request)
    {
        var employeeId = await GetCurrentEmployeeIdAsync();
        if (employeeId is null)
            return BadRequest(new { message = "No employee profile linked to this user." });

        var dto = await _leaveService.SubmitAsync(employeeId.Value, request);
        return Created(string.Empty, dto);
    }

    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = "Admin,HrManager")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var reviewerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var dto = await _leaveService.ApproveAsync(id, reviewerId);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "Admin,HrManager")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ReviewLeaveRequest request)
    {
        var reviewerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var dto = await _leaveService.RejectAsync(id, reviewerId, request.Reason);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var employeeId = await GetCurrentEmployeeIdAsync();
        if (employeeId is null) return Forbid();

        var cancelled = await _leaveService.CancelAsync(id, employeeId.Value);
        return cancelled ? NoContent() : NotFound();
    }

    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendar([FromQuery] DateOnly from, [FromQuery] DateOnly to)
        => Ok(await _leaveService.GetCalendarAsync(from, to));

    [HttpGet("balances/{employeeId:guid}")]
    public async Task<IActionResult> GetBalances(Guid employeeId, [FromQuery] int? year)
        => Ok(await _leaveService.GetBalancesAsync(employeeId, year ?? DateTime.UtcNow.Year));

    private async Task<Guid?> GetCurrentEmployeeIdAsync()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        return user?.EmployeeId;
    }
}
