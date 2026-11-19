using company_resource_manager.Server.DTOs.Analytics;

namespace company_resource_manager.Server.Services.Interfaces;

public interface ICrmAnalyticsService
{
    Task<CrmLeadsDto> GetLeadsAsync();
    Task<CrmRevenueDto> GetRevenueAsync();
    Task<CrmClientsDto> GetClientsAsync();
}
