namespace company_resource_manager.Server.DTOs.Estimation;

public record EstimateProjectResponse(
    bool IsModelReady,
    decimal? EstimatedCost,
    int? EstimatedDurationDays,
    float CostModelR2,
    float DurationModelR2,
    string ModelVersion,
    string? Message
);
