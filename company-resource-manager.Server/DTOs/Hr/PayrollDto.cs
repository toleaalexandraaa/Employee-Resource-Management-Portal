using company_resource_manager.Server.Models.Hr;

namespace company_resource_manager.Server.DTOs.Hr;

public record PayrollRecordDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    int Month,
    int Year,
    decimal GrossSalary,
    decimal BonusAmount,
    int MealTicketCount,
    decimal MealTicketValue,
    decimal TaxDeductions,
    decimal SocialContributions,
    decimal NetSalary,
    DateTime GeneratedAt,
    PayrollStatus Status);

public record GeneratePayrollRequest(
    Guid EmployeeId,
    int Month,
    int Year,
    decimal BonusAmount,
    int MealTicketCount,
    decimal MealTicketValue);
