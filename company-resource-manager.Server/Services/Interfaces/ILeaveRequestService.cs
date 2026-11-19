using company_resource_manager.Server.DTOs;
using company_resource_manager.Server.DTOs.Hr;

namespace company_resource_manager.Server.Services.Interfaces;

public interface ILeaveRequestService
{
    Task<PagedResult<LeaveRequestDto>> GetAllAsync(Guid? employeeId, int page, int pageSize);
    Task<LeaveRequestDto?> GetByIdAsync(Guid id);
    Task<LeaveRequestDto> SubmitAsync(Guid employeeId, CreateLeaveRequest request);
    Task<LeaveRequestDto?> ApproveAsync(Guid id, Guid reviewerUserId);
    Task<LeaveRequestDto?> RejectAsync(Guid id, Guid reviewerUserId, string? reason);
    Task<bool> CancelAsync(Guid id, Guid employeeId);
    Task<IReadOnlyList<LeaveRequestDto>> GetCalendarAsync(DateOnly from, DateOnly to);
    Task<IReadOnlyList<LeaveBalanceDto>> GetBalancesAsync(Guid employeeId, int year);
}
