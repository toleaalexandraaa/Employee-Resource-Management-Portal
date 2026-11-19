using company_resource_manager.Server.DTOs;
using company_resource_manager.Server.DTOs.Crm;

namespace company_resource_manager.Server.Services.Interfaces;

public interface IClientService
{
    Task<PagedResult<ClientListItemDto>> GetAllAsync(int page, int pageSize);
    Task<ClientDto?> GetByIdAsync(Guid id);
    Task<ClientDto> CreateAsync(CreateClientRequest request);
    Task<ClientDto?> UpdateAsync(Guid id, UpdateClientRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<IReadOnlyList<ClientContactDto>> GetContactsAsync(Guid clientId);
    Task<ClientContactDto> AddContactAsync(Guid clientId, CreateClientContactRequest request);
    Task<ClientContactDto?> UpdateContactAsync(Guid clientId, Guid contactId, UpdateClientContactRequest request);
    Task<bool> RemoveContactAsync(Guid clientId, Guid contactId);
    Task<ClientHistoryDto?> GetHistoryAsync(Guid clientId);
}
