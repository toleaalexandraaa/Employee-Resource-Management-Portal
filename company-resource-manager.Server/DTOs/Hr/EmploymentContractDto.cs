using company_resource_manager.Server.Models.Hr;

namespace company_resource_manager.Server.DTOs.Hr;

public record EmploymentContractDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    ContractType ContractType,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal GrossSalary,
    string Currency,
    bool IsActive);

public record CreateContractRequest(
    Guid EmployeeId,
    ContractType ContractType,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal GrossSalary,
    string Currency = "RON");

public record UpdateContractRequest(
    ContractType ContractType,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal GrossSalary,
    string Currency,
    bool IsActive);
