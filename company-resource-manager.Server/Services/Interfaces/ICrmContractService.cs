using company_resource_manager.Server.DTOs;
using company_resource_manager.Server.DTOs.Crm;

namespace company_resource_manager.Server.Services.Interfaces;

public interface ICrmContractService
{
    Task<PagedResult<CrmContractDto>> GetAllAsync(int page, int pageSize);
    Task<CrmContractDto?> GetByIdAsync(Guid id);
    Task<CrmContractDto> CreateAsync(CreateCrmContractRequest request);
    Task<CrmContractDto?> UpdateAsync(Guid id, UpdateCrmContractRequest request);
    Task<CrmContractDto?> SignAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
}
