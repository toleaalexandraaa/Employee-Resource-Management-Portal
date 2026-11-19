using company_resource_manager.Server.DTOs;
using company_resource_manager.Server.DTOs.Proj;
using company_resource_manager.Server.Models.Proj;
using TaskStatus = company_resource_manager.Server.Models.Proj.TaskStatus;

namespace company_resource_manager.Server.Services.Interfaces;

public interface ITaskService
{
    Task<PagedResult<ProjectTaskDto>> GetAllAsync(
        Guid? projectId, Guid? assigneeId, TaskStatus? status, int page, int pageSize);
    Task<ProjectTaskDto?> GetByIdAsync(Guid id);
    Task<ProjectTaskDto> CreateAsync(CreateTaskRequest request, Guid createdByUserId);
    Task<ProjectTaskDto?> UpdateAsync(Guid id, UpdateTaskRequest request);
    Task<ProjectTaskDto?> MoveAsync(Guid id, MoveTaskRequest request);
    Task<bool> DeleteAsync(Guid id);
}
