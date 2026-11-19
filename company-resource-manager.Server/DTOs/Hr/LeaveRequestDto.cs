using company_resource_manager.Server.Models.Hr;

namespace company_resource_manager.Server.DTOs.Hr;

public record LeaveRequestDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    LeaveType LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    int WorkingDaysCount,
    string? Reason,
    LeaveStatus Status,
    Guid? ReviewedBy,
    DateTime? ReviewedAt);

public record CreateLeaveRequest(
    LeaveType LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    int WorkingDaysCount,
    string? Reason);

public record ReviewLeaveRequest(string? Reason);
