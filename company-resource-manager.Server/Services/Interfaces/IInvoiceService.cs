using company_resource_manager.Server.DTOs;
using company_resource_manager.Server.DTOs.Crm;

namespace company_resource_manager.Server.Services.Interfaces;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceListItemDto>> GetAllAsync(int page, int pageSize);
    Task<InvoiceDto?> GetByIdAsync(Guid id);
    Task<InvoiceDto> CreateAsync(CreateInvoiceRequest request);
    Task<InvoiceDto> GenerateFromTimeEntriesAsync(GenerateInvoiceFromTimeEntriesRequest request);
    Task<InvoiceDto?> SendAsync(Guid id);
    Task<InvoiceDto?> RecordPaymentAsync(Guid id, RecordPaymentRequest request);
    Task<bool> DeleteAsync(Guid id);
}
