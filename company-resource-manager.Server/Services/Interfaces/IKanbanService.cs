using company_resource_manager.Server.DTOs.Proj;

namespace company_resource_manager.Server.Services.Interfaces;

public interface IKanbanService
{
    Task<KanbanBoardDto?> GetBoardAsync(Guid projectId);
    Task ReorderColumnsAsync(Guid projectId, ReorderColumnsRequest request);
}
