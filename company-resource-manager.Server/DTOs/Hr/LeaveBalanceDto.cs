using company_resource_manager.Server.Models.Hr;

namespace company_resource_manager.Server.DTOs.Hr;

public record LeaveBalanceDto(
    Guid Id,
    Guid EmployeeId,
    int Year,
    LeaveType LeaveType,
    int TotalDays,
    int UsedDays,
    int RemainingDays);
