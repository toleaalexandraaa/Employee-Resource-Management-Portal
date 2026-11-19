using company_resource_manager.Server.DTOs;
using company_resource_manager.Server.DTOs.Hr;

namespace company_resource_manager.Server.Services.Interfaces;

public interface IPayrollService
{
    Task<PagedResult<PayrollRecordDto>> GetAllAsync(int page, int pageSize);
    Task<PayrollRecordDto?> GetByIdAsync(Guid id);
    Task<PayrollRecordDto> GenerateAsync(GeneratePayrollRequest request);
    Task<PayrollRecordDto?> ApproveAsync(Guid id, Guid approverUserId);
    Task<PayrollRecordDto?> MarkAsPaidAsync(Guid id);
}
