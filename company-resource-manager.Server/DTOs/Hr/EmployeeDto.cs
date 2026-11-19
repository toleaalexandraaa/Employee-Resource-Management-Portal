namespace company_resource_manager.Server.DTOs.Hr;

public record EmployeeListItemDto(
    Guid Id,
    string FullName,
    string Department,
    string JobTitle,
    string WorkEmail,
    bool IsActive,
    float SeniorityYears);

public record CreateEmployeeResult(EmployeeDto Employee, string TemporaryPassword);

public record EmployeeDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string PersonalEmail,
    string WorkEmail,
    string Phone,
    DateOnly BirthDate,
    DateOnly HireDate,
    string Department,
    string JobTitle,
    Guid? ManagerId,
    string? ManagerName,
    float SeniorityYears,
    bool IsActive);
