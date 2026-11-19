using company_resource_manager.Server.DTOs.Hr;

namespace company_resource_manager.Server.Services.Interfaces;

public interface IEmploymentContractService
{
    Task<IReadOnlyList<EmploymentContractDto>> GetByEmployeeAsync(Guid employeeId);
    Task<EmploymentContractDto> CreateAsync(CreateContractRequest request);
    Task<EmploymentContractDto?> UpdateAsync(Guid id, UpdateContractRequest request);
    Task<bool> DeleteAsync(Guid id);
}
