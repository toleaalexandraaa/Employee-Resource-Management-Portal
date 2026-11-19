using company_resource_manager.Server.DTOs.Analytics;

namespace company_resource_manager.Server.Services.Interfaces;

public interface IOverviewAnalyticsService
{
    Task<OverviewDto> GetOverviewAsync();
}
