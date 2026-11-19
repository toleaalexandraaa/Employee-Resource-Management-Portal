namespace company_resource_manager.Server.DTOs.Analytics;

public record LabelValueDto(string Label, decimal Value);

// ── Workforce ─────────────────────────────────────────────────────────────────

public record WorkforceDto(
    IReadOnlyList<LabelValueDto> EmployeesByDepartment,
    IReadOnlyList<LabelValueDto> EmployeesByContractType,
    IReadOnlyList<LabelValueDto> SeniorityDistribution,
    IReadOnlyList<MonthlyValueDto> HiresLast24Months
);

// ── Leave ─────────────────────────────────────────────────────────────────────

public record LeaveBalanceItemDto(string LeaveType, int TotalDays, int UsedDays);

public record PendingLeaveItemDto(
    Guid Id,
    string EmployeeName,
    string LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    int WorkingDays,
    DateTime SubmittedAt
);

public record DepartmentHeatmapRowDto(string Department, IReadOnlyList<decimal> Values);

public record HrLeaveDto(
    IReadOnlyList<LeaveBalanceItemDto> BalanceUtilization,
    IReadOnlyList<string> LeaveMonthLabels,
    IReadOnlyList<string> LeaveTypeLabels,
    IReadOnlyList<IReadOnlyList<decimal>> LeaveByMonthMatrix,
    int PendingCount,
    IReadOnlyList<PendingLeaveItemDto> PendingRequests,
    IReadOnlyList<string> HeatmapMonthLabels,
    IReadOnlyList<DepartmentHeatmapRowDto> LeaveByDepartment
);

// ── Payroll ───────────────────────────────────────────────────────────────────

public record PayrollCompositionDto(
    decimal AvgGross,
    decimal AvgBonus,
    decimal AvgMealTickets,
    decimal AvgTax,
    decimal AvgSocial,
    decimal AvgNet
);

public record HrPayrollDto(
    IReadOnlyList<MonthlyValueDto> CostLast12Months,
    IReadOnlyList<LabelValueDto> AvgSalaryByDepartment,
    PayrollCompositionDto Composition,
    int CurrentMonthDraft,
    int CurrentMonthApproved,
    int CurrentMonthPaid
);
