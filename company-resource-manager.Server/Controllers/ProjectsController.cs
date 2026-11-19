using System.Security.Claims;
using company_resource_manager.Server.Data;
using company_resource_manager.Server.DTOs.Proj;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace company_resource_manager.Server.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projects;
    private readonly AppDbContext _db;

    public ProjectsController(IProjectService projects, AppDbContext db)
    {
        _projects = projects;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var employeeId = await GetEmployeeIdIfRestrictedAsync();
        return Ok(await _projects.GetAllAsync(page, pageSize, employeeId));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var employeeId = await GetEmployeeIdIfRestrictedAsync();
        if (employeeId.HasValue)
        {
            var isMember = await _db.ProjectMembers
                .AnyAsync(m => m.ProjectId == id && m.EmployeeId == employeeId.Value);
            if (!isMember) return Forbid();
        }
        var dto = await _projects.GetByIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>
    /// Returns the EmployeeId for users whose access must be scoped to their own projects
    /// (i.e. Employee role only, not Admin or ProjectManager). Returns null for privileged roles.
    /// </summary>
    private async Task<Guid?> GetEmployeeIdIfRestrictedAsync()
    {
        if (User.IsInRole("Admin") || User.IsInRole("ProjectManager") || User.IsInRole("HrManager") || User.IsInRole("AccountManager"))
            return null;
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        return user?.EmployeeId;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var dto = await _projects.CreateAsync(request, userId);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequest request)
    {
        var dto = await _projects.UpdateAsync(id, request);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _projects.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid id)
        => Ok(await _projects.GetMembersAsync(id));

    [HttpPost("{id:guid}/members")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddProjectMemberRequest request)
    {
        var dto = await _projects.AddMemberAsync(id, request);
        return Created(string.Empty, dto);
    }

    [HttpDelete("{id:guid}/members/{memberId:guid}")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid memberId)
    {
        var removed = await _projects.RemoveMemberAsync(id, memberId);
        return removed ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/gantt")]
    public async Task<IActionResult> GetGantt(Guid id)
        => Ok(await _projects.GetGanttAsync(id));
}
