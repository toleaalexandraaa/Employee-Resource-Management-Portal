namespace company_resource_manager.Server.DTOs.Analytics;

public record EmployeeClusterPoint(
    Guid EmployeeId,
    string EmployeeName,
    float HoursLogged,
    float TasksCompleted,
    float HoursVsEstimatedRatio,
    float ProjectsActive,
    float SeniorityYears,
    int Cluster,
    string ClusterLabel
);

public record EmployeeClustersDto(
    List<EmployeeClusterPoint> Employees,
    bool HasSufficientData,
    string Message
);

public record ClientSegmentPoint(
    Guid ClientId,
    string ClientName,
    float TotalRevenue,
    float ProjectCount,
    float AvgPaymentDelayDays,
    float OpenInvoiceAmount,
    float LeadCount,
    int Cluster,
    string ClusterLabel
);

public record ClientSegmentsDto(
    List<ClientSegmentPoint> Clients,
    bool HasSufficientData,
    string Message
);

public record ProjectRiskScore(
    Guid ProjectId,
    string ProjectName,
    int RiskScore,
    string RiskLevel,
    float BudgetBurnRate,
    float TimeBurnRate,
    float TimeElapsedRatio,
    int OverdueTaskCount
);

public record ProjectRisksDto(
    List<ProjectRiskScore> Projects,
    bool HasSufficientData,
    string Message
);
