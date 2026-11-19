using company_resource_manager.Server.DTOs.Analytics;

namespace company_resource_manager.Server.Services.Interfaces;

public interface IProjectAnalyticsService
{
    Task<ProjectHealthDto> GetHealthAsync();
    Task<ProjectTimeDto> GetTimeAsync();
    Task<ProjectTasksDto> GetTasksAsync();
}
