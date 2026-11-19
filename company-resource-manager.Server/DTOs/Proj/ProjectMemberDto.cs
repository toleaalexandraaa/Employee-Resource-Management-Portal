namespace company_resource_manager.Server.DTOs.Proj;

public record ProjectMemberDto(
    Guid Id,
    Guid ProjectId,
    Guid EmployeeId,
    string EmployeeName,
    string Role,
    decimal AllocatedHoursPerWeek,
    DateOnly JoinDate,
    DateOnly? LeaveDate);

public record AddProjectMemberRequest(
    Guid EmployeeId,
    string Role,
    decimal AllocatedHoursPerWeek,
    DateOnly JoinDate,
    DateOnly? LeaveDate);
