using company_resource_manager.Server.DTOs.Analytics;

namespace company_resource_manager.Server.Services.Interfaces;

public interface IHrAnalyticsService
{
    Task<WorkforceDto> GetWorkforceAsync();
    Task<HrLeaveDto> GetLeaveAsync();
    Task<HrPayrollDto> GetPayrollAsync();
}
