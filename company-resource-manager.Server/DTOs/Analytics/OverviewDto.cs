namespace company_resource_manager.Server.DTOs.Analytics;

public record MonthlyValueDto(string Label, decimal Value);

public record OverviewDto(
    int ActiveEmployees,
    decimal PayrollThisMonth,
    int ActiveProjects,
    double BudgetVariancePct,
    int OpenLeadsCount,
    decimal OpenLeadsValue,
    decimal RevenueThisMonth,
    int OverdueInvoicesCount,
    decimal OverdueInvoicesAmount,
    int PendingLeaveRequests,
    IReadOnlyList<MonthlyValueDto> RevenueLast12Months,
    IReadOnlyList<MonthlyValueDto> HiresLast6Months
);
