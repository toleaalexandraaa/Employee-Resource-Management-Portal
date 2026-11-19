using company_resource_manager.Server.DTOs;
using company_resource_manager.Server.DTOs.Proj;

namespace company_resource_manager.Server.Services.Interfaces;

public interface IProjectService
{
    Task<PagedResult<ProjectListItemDto>> GetAllAsync(int page, int pageSize, Guid? memberEmployeeId = null);
    Task<ProjectDto?> GetByIdAsync(Guid id);
    Task<ProjectDto> CreateAsync(CreateProjectRequest request, Guid managerUserId);
    Task<ProjectDto?> UpdateAsync(Guid id, UpdateProjectRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<IReadOnlyList<ProjectMemberDto>> GetMembersAsync(Guid projectId);
    Task<ProjectMemberDto> AddMemberAsync(Guid projectId, AddProjectMemberRequest request);
    Task<bool> RemoveMemberAsync(Guid projectId, Guid memberId);
    Task<IReadOnlyList<GanttTaskDto>> GetGanttAsync(Guid projectId);
}
