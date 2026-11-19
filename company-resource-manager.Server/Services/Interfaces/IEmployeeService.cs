using company_resource_manager.Server.DTOs;
using company_resource_manager.Server.DTOs.Hr;

namespace company_resource_manager.Server.Services.Interfaces;

public interface IEmployeeService
{
    Task<PagedResult<EmployeeListItemDto>> GetAllAsync(int page, int pageSize);
    Task<EmployeeDto?> GetByIdAsync(Guid id);
    Task<CreateEmployeeResult> CreateAsync(CreateEmployeeRequest request);
    Task<EmployeeDto?> UpdateAsync(Guid id, UpdateEmployeeRequest request);
    Task<bool> DeleteAsync(Guid id);
}
