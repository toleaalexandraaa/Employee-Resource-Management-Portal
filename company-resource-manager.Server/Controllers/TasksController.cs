using System.Security.Claims;
using company_resource_manager.Server.DTOs.Proj;
using company_resource_manager.Server.Models.Proj;
using TaskStatus = company_resource_manager.Server.Models.Proj.TaskStatus;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace company_resource_manager.Server.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _tasks;

    public TasksController(ITaskService tasks) => _tasks = tasks;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? assigneeId,
        [FromQuery] TaskStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
        => Ok(await _tasks.GetAllAsync(projectId, assigneeId, status, page, pageSize));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dto = await _tasks.GetByIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var dto = await _tasks.CreateAsync(request, userId);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest request)
    {
        var dto = await _tasks.UpdateAsync(id, request);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("{id:guid}/move")]
    public async Task<IActionResult> Move(Guid id, [FromBody] MoveTaskRequest request)
    {
        var dto = await _tasks.MoveAsync(id, request);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _tasks.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
