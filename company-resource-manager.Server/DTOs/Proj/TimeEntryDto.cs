namespace company_resource_manager.Server.DTOs.Proj;

public record TimeEntryDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    Guid? TaskId,
    string? TaskTitle,
    Guid ProjectId,
    string ProjectName,
    DateOnly Date,
    decimal HoursLogged,
    string? Description);

public record CreateTimeEntryRequest(
    Guid? TaskId,
    Guid ProjectId,
    DateOnly Date,
    decimal HoursLogged,
    string? Description);

public record UpdateTimeEntryRequest(
    DateOnly Date,
    decimal HoursLogged,
    string? Description);
