using company_resource_manager.Server.DTOs;
using company_resource_manager.Server.DTOs.Crm;

namespace company_resource_manager.Server.Services.Interfaces;

public interface ILeadService
{
    Task<PagedResult<LeadDto>> GetAllAsync(int page, int pageSize);
    Task<LeadDto?> GetByIdAsync(Guid id);
    Task<LeadDto> CreateAsync(CreateLeadRequest request);
    Task<LeadDto?> UpdateAsync(Guid id, UpdateLeadRequest request);
    Task<LeadDto?> AdvanceStageAsync(Guid id);
    Task<ClientDto> ConvertToClientAsync(Guid id, ConvertLeadRequest request);
    Task<bool> DeleteAsync(Guid id);
}
