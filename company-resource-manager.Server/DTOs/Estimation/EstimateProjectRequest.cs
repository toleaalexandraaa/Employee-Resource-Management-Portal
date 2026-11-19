namespace company_resource_manager.Server.DTOs.Estimation;

public record EstimateProjectRequest(
    int TeamSize,
    float EstimatedHoursTotal,
    int TaskCount,
    bool HasFixedDeadline,
    float AverageSeniorityYears,
    bool IsFirstProjectWithClient
);
