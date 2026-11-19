namespace company_resource_manager.Server.DTOs.Analytics;

// ── Health ────────────────────────────────────────────────────────────────────

public record ProjectBudgetVarianceDto(string ProjectName, decimal Variance);

public record DurationScatterPointDto(string ProjectName, int EstimatedDays, int ActualDays);

public record ProjectHealthDto(
    IReadOnlyList<LabelValueDto> ByStatus,
    IReadOnlyList<ProjectBudgetVarianceDto> BudgetVariances,
    int CompletedCount,
    int OnTimeCount,
    IReadOnlyList<string> LateProjectNames,
    IReadOnlyList<DurationScatterPointDto> DurationScatter
);

// ── Time ──────────────────────────────────────────────────────────────────────

public record ProjectHoursDto(string ProjectName, decimal EstimatedHours, decimal LoggedHours);

public record MemberUtilizationDto(string MemberName, decimal AllocatedHoursPerWeek, decimal LoggedHoursThisWeek);

public record ProjectTimeDto(
    IReadOnlyList<ProjectHoursDto> HoursByProject,
    IReadOnlyList<MemberUtilizationDto> TeamUtilization,
    IReadOnlyList<LabelValueDto> HoursByDayOfWeek,
    IReadOnlyList<MonthlyValueDto> WeeklyHours
);

// ── Tasks ─────────────────────────────────────────────────────────────────────

public record WipViolationDto(string ProjectName, string ColumnName, int WipLimit, int CurrentCount);

public record ProjectTasksDto(
    IReadOnlyList<LabelValueDto> TasksByStatus,
    IReadOnlyList<LabelValueDto> TasksByPriority,
    IReadOnlyList<LabelValueDto> AvgCycleTimeByPriority,
    IReadOnlyList<WipViolationDto> WipViolations,
    IReadOnlyList<MonthlyValueDto> CompletionVelocity
);
