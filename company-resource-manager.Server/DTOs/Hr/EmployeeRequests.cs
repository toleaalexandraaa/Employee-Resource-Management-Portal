namespace company_resource_manager.Server.DTOs.Hr;

public record CreateEmployeeRequest(
    string FirstName,
    string LastName,
    string PersonalEmail,
    string WorkEmail,
    string Phone,
    DateOnly BirthDate,
    DateOnly HireDate,
    string Department,
    string JobTitle,
    Guid? ManagerId,
    float SeniorityYears,
    string Role);

public record UpdateEmployeeRequest(
    string FirstName,
    string LastName,
    string PersonalEmail,
    string WorkEmail,
    string Phone,
    string Department,
    string JobTitle,
    Guid? ManagerId,
    float SeniorityYears,
    bool IsActive);
