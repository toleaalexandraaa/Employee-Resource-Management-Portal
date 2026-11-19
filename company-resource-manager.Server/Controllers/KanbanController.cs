using company_resource_manager.Server.DTOs.Proj;
using company_resource_manager.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace company_resource_manager.Server.Controllers;

[ApiController]
[Route("api/kanban")]
[Authorize]
public class KanbanController : ControllerBase
{
    private readonly IKanbanService _kanban;

    public KanbanController(IKanbanService kanban) => _kanban = kanban;

    [HttpGet("{projectId:guid}")]
    public async Task<IActionResult> GetBoard(Guid projectId)
    {
        var dto = await _kanban.GetBoardAsync(projectId);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("{projectId:guid}/columns")]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> ReorderColumns(Guid projectId, [FromBody] ReorderColumnsRequest request)
    {
        await _kanban.ReorderColumnsAsync(projectId, request);
        return NoContent();
    }
}
