using company_resource_manager.Server.DTOs;
using company_resource_manager.Server.DTOs.Proj;

namespace company_resource_manager.Server.Services.Interfaces;

public interface ITimeEntryService
{
    Task<PagedResult<TimeEntryDto>> GetAllAsync(
        Guid? employeeId, Guid? projectId, DateOnly? from, DateOnly? to, int page, int pageSize);
    Task<TimeEntryDto> CreateAsync(CreateTimeEntryRequest request, Guid employeeId);
    Task<TimeEntryDto?> UpdateAsync(Guid id, UpdateTimeEntryRequest request, Guid employeeId);
    Task<bool> DeleteAsync(Guid id, Guid employeeId);
    Task<decimal> GetProjectTotalHoursAsync(Guid projectId);
}
